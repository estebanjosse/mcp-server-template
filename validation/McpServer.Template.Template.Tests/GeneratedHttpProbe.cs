using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace McpServer.Template.Template.Tests;

internal static class GeneratedHttpProbe
{
    private const string Modern = "2026-07-28";
    private const string Welcome = "resource://welcome";
    private const string Status = "resource://status";
    private static readonly object ClientInfo = new { name = "generated-http-probe", version = "1.0" };

    public static async Task VerifyAsync(string hostDll, string artifacts, bool samples)
    {
        Assert.True(Path.IsPathFullyQualified(hostDll) && File.Exists(hostDll), $"Missing generated HTTP host: {hostDll}");
        Directory.CreateDirectory(artifacts);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(hostDll)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(hostDll);
        start.ArgumentList.Add("--urls=http://127.0.0.1:0");
        // Use the shipped auth/config defaults, not the runner's environment.
        foreach (var key in start.Environment.Keys.ToArray())
        {
            var setting = key.Replace("__", ":", StringComparison.Ordinal);
            if (setting.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase) ||
                setting.StartsWith("Authentication:", StringComparison.OrdinalIgnoreCase) ||
                setting.StartsWith("Kestrel:", StringComparison.OrdinalIgnoreCase) ||
                setting.StartsWith("Logging:", StringComparison.OrdinalIgnoreCase) ||
                setting.StartsWith("MCP_AUTH_", StringComparison.OrdinalIgnoreCase) ||
                setting.Equals("DOTNET_CONTENTROOT", StringComparison.OrdinalIgnoreCase) ||
                setting.Equals("DOTNET_LAUNCH_PROFILE", StringComparison.OrdinalIgnoreCase))
                start.Environment.Remove(key);
        }
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        // The status handler formats timestamps using its own process culture.
        // Pin its calendar without changing the test runner's culture or the generated app.
        start.Environment["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
        start.ArgumentList.Add("--Logging:LogLevel:Microsoft.Hosting.Lifetime=Information");

        await using var stdout = new StreamWriter(Path.Combine(artifacts, "stdout.log")) { AutoFlush = true };
        await using var stderr = File.Create(Path.Combine(artifacts, "stderr.log"));
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {hostDll}.");
        using var drains = new CancellationTokenSource();
        var listening = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = DrainOutputAsync(process.StandardOutput, stdout, listening, drains.Token);
        var error = process.StandardError.BaseStream.CopyToAsync(stderr, drains.Token);
        var exit = process.WaitForExitAsync();
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var ready = await Task.WhenAny(listening.Task, exit, output, error).WaitAsync(deadline.Token);
            if (ready == output || ready == error) await ready;
            Assert.True(listening.Task.IsCompletedSuccessfully && !process.HasExited,
                $"Generated HTTP host stopped before advertising its loopback address. Logs: {artifacts}");
            using var client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                UseProxy = false
            }) { BaseAddress = await listening.Task };
            await using var trace = new StreamWriter(Path.Combine(artifacts, "client.jsonl")) { AutoFlush = true };
            using var health = await client.GetAsync("/health", deadline.Token);
            var healthBody = await health.Content.ReadAsStringAsync(deadline.Token);
            await TraceAsync(trace, new { path = "/health", status = (int)health.StatusCode, body = healthBody }, deadline.Token);
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            using (var healthJson = JsonDocument.Parse(healthBody))
                Assert.Equal("Healthy", healthJson.RootElement.GetProperty("status").GetString());

            foreach (var version in new[] { Modern, "2025-11-25", "2025-06-18" })
                await VerifyRevisionAsync(client, trace, version, samples, deadline.Token);
            Assert.False(process.HasExited, $"Generated HTTP host exited during the probe. Logs: {artifacts}");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Generated HTTP probe failed. Evidence: {artifacts}", exception);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) when (process.HasExited) { }
                }
                await Task.WhenAll(exit, output, error).WaitAsync(TimeSpan.FromSeconds(5));
                await File.WriteAllTextAsync(Path.Combine(artifacts, "exit.log"), $"Exit code: {process.ExitCode}");
            }
            finally
            {
                drains.Cancel();
            }
        }
    }

    private static async Task DrainOutputAsync(StreamReader reader, StreamWriter log,
        TaskCompletionSource<Uri> listening, CancellationToken cancellationToken)
    {
        const string marker = "Now listening on: ";
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            await log.WriteLineAsync(line.AsMemory(), cancellationToken);
            var index = line.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0 && Uri.TryCreate(line[(index + marker.Length)..].Trim(), UriKind.Absolute, out var address) &&
                address.Scheme == Uri.UriSchemeHttp && address.Host == "127.0.0.1" && address.Port > 0)
                listening.TrySetResult(address);
        }
    }

    private static async Task VerifyRevisionAsync(HttpClient client, StreamWriter trace, string version, bool samples,
        CancellationToken cancellationToken)
    {
        var modern = version == Modern;
        var nextId = 0;

        async Task<JsonElement> Call(string method, Dictionary<string, object>? parameters = null, string? name = null)
        {
            parameters ??= new();
            if (modern)
            {
                parameters["_meta"] = new Dictionary<string, object>
                {
                    ["io.modelcontextprotocol/protocolVersion"] = version,
                    ["io.modelcontextprotocol/clientInfo"] = ClientInfo,
                    ["io.modelcontextprotocol/clientCapabilities"] = new { }
                };
            }
            var result = await ExchangeAsync(client, trace, version, ++nextId, method, parameters, name, cancellationToken);
            if (modern && method != "server/discover")
                Assert.Equal("complete", result.GetProperty("resultType").GetString());
            else if (!modern)
                Assert.False(result.TryGetProperty("resultType", out _));
            return result;
        }

        JsonElement capabilities;
        if (modern)
        {
            // Prove a direct call works before discovery, not just after a handshake.
            AssertNames(await Call("prompts/list"), "prompts", "name", "greeting");
            var discovery = await Call("server/discover");
            Assert.Contains(version, discovery.GetProperty("supportedVersions").EnumerateArray().Select(v => v.GetString()));
            capabilities = discovery.GetProperty("capabilities");
        }
        else
        {
            var initialized = await Call("initialize", new()
            {
                ["protocolVersion"] = version, ["capabilities"] = new { }, ["clientInfo"] = ClientInfo
            });
            Assert.Equal(version, initialized.GetProperty("protocolVersion").GetString());
            capabilities = initialized.GetProperty("capabilities");
            await ExchangeAsync(client, trace, version, null, "notifications/initialized", new { }, null, cancellationToken);
            AssertNames(await Call("prompts/list"), "prompts", "name", "greeting");
        }

        Assert.Equal(JsonValueKind.Object, capabilities.GetProperty("prompts").ValueKind);
        Assert.Equal(JsonValueKind.Object, capabilities.GetProperty("resources").ValueKind);
        Assert.Equal(samples, capabilities.TryGetProperty("tools", out var tools));

        const string greeting = "greeting";
        var prompt = await Call("prompts/get", new() { ["name"] = greeting, ["arguments"] = new { language = "fr" } }, greeting);
        var message = Assert.Single(prompt.GetProperty("messages").EnumerateArray());
        Assert.Equal("user", message.GetProperty("role").GetString());
        Assert.Equal("Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.",
            Text(message.GetProperty("content")));

        var resources = await Call("resources/list");
        AssertResourceCache(resources, version, "resources/list");
        AssertNames(resources, "resources", "uri", Status, Welcome);
        var welcome = await ReadResource(Welcome, "text/plain");
        Assert.StartsWith("Welcome to ", welcome);
        Assert.Contains("This is a demonstration of a clean, scalable Model Context Protocol (MCP) server", welcome);
        var firstStatus = await ReadFreshStatus();
        // Cross the wire timestamp's one-second boundary before reading again so a
        // cached copy of the first response cannot pass the freshness check.
        var nextSecond = firstStatus.AddSeconds(1);
        while (nextSecond - DateTimeOffset.UtcNow is { Ticks: > 0 } remaining)
            await Task.Delay(remaining, cancellationToken);
        Assert.True(await ReadFreshStatus() > firstStatus, $"{version} status timestamp did not advance.");

        if (!samples) return;
        Assert.Equal(JsonValueKind.Object, tools.ValueKind);
        AssertNames(await Call("tools/list"), "tools", "name", "calc_divide", "echo");
        const string echoMessage = "HTTP probe: café ☃\nsecond line";
        var before = DateTimeOffset.UtcNow;
        var echo = SuccessfulDto(await Tool("echo", new { message = echoMessage }));
        var after = DateTimeOffset.UtcNow;
        Assert.Equal(echoMessage, echo.GetProperty("message").GetString());
        var stamp = echo.GetProperty("timestamp").GetString()!;
        Assert.True(stamp.EndsWith('Z') || stamp.EndsWith("+00:00", StringComparison.Ordinal), stamp);
        Assert.InRange(DateTimeOffset.Parse(stamp, CultureInfo.InvariantCulture), before, after);
        var divide = SuccessfulDto(await Tool("calc_divide", new { a = 7, b = 2 }));
        Assert.Equal(3.5, divide.GetProperty("result").GetDouble());
        var failed = await Tool("calc_divide", new { a = 7, b = 0 });
        Assert.True(failed.GetProperty("isError").GetBoolean());
        Assert.Equal("An error occurred invoking 'calc_divide': Division by zero is not allowed",
            Text(Assert.Single(failed.GetProperty("content").EnumerateArray())));

        async Task<string> ReadResource(string uri, string mimeType)
        {
            var result = await Call("resources/read", new() { ["uri"] = uri }, uri);
            AssertResourceCache(result, version, $"resources/read {uri}");
            var content = Assert.Single(result.GetProperty("contents").EnumerateArray());
            Assert.Equal(uri, content.GetProperty("uri").GetString());
            Assert.Equal(mimeType, content.GetProperty("mimeType").GetString());
            return content.GetProperty("text").GetString()!;
        }

        async Task<DateTimeOffset> ReadFreshStatus()
        {
            var before = DateTimeOffset.UtcNow;
            var text = await ReadResource(Status, "application/json");
            var after = DateTimeOffset.UtcNow;
            using var status = JsonDocument.Parse(text);
            Assert.Equal("Running", status.RootElement.GetProperty("Status").GetString());
            Assert.True(status.RootElement.TryGetProperty("Timestamp", out var stamp) && stamp.ValueKind == JsonValueKind.String,
                $"{version} status is missing a timestamp: {text}");
            Assert.True(DateTimeOffset.TryParseExact(stamp.GetString(), "yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var timestamp), $"{version} status timestamp is not UTC: {stamp}");
            // The handler drops fractional seconds; truncate both client clock bounds to match.
            Assert.InRange(timestamp, DateTimeOffset.FromUnixTimeSeconds(before.ToUnixTimeSeconds()),
                DateTimeOffset.FromUnixTimeSeconds(after.ToUnixTimeSeconds()));
            return timestamp;
        }

        Task<JsonElement> Tool(string name, object arguments) =>
            Call("tools/call", new() { ["name"] = name, ["arguments"] = arguments }, name);
    }

    private static void AssertResourceCache(JsonElement result, string version, string operation)
    {
        var hasTtl = result.TryGetProperty("ttlMs", out var ttl);
        var hasScope = result.TryGetProperty("cacheScope", out var scope);
        if (version == Modern)
        {
            Assert.True(hasTtl && ttl.ValueKind == JsonValueKind.Number && ttl.TryGetInt32(out var milliseconds) && milliseconds == 0,
                $"{version} {operation} must specify ttlMs=0: {result}");
            Assert.True(hasScope && scope.ValueKind == JsonValueKind.String && scope.GetString() == "private",
                $"{version} {operation} must specify cacheScope=private: {result}");
        }
        else
        {
            Assert.False(hasTtl || hasScope, $"{version} {operation} must omit modern cache fields: {result}");
        }
    }

    private static async Task<JsonElement> ExchangeAsync(HttpClient client, StreamWriter trace, string version, int? id,
        string method, object parameters, string? name, CancellationToken cancellationToken)
    {
        var message = new Dictionary<string, object> { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = parameters };
        if (id is int requestId) message["id"] = requestId;
        var payload = JsonSerializer.Serialize(message);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (method != "initialize") request.Headers.Add("MCP-Protocol-Version", version);
        if (version == Modern)
        {
            request.Headers.Add("Mcp-Method", method);
            if (name is not null) request.Headers.Add("Mcp-Name", name);
        }
        await TraceAsync(trace, new { direction = "request", version, method, id, name, body = payload }, cancellationToken);
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var hasSession = response.Headers.Contains("Mcp-Session-Id");
        await TraceAsync(trace, new { direction = "response", version, method, id, status = (int)response.StatusCode, mediaType, hasSession, body },
            cancellationToken);
        Assert.False(hasSession, $"{version} {method} returned a session ID on the stateless endpoint.");
        Assert.False(response.Headers.Contains("WWW-Authenticate"));
        Assert.Equal(id is null ? HttpStatusCode.Accepted : HttpStatusCode.OK, response.StatusCode);
        if (id is null)
        {
            Assert.Empty(body);
            return default;
        }

        Assert.True(mediaType is "application/json" or "text/event-stream", $"Unexpected MCP media type: {mediaType}");
        using var json = JsonDocument.Parse(mediaType == "text/event-stream" ? ReadSseData(body) : body);
        var reply = json.RootElement;
        Assert.Equal("2.0", reply.GetProperty("jsonrpc").GetString());
        Assert.Equal(id.Value, reply.GetProperty("id").GetInt32());
        Assert.False(reply.TryGetProperty("error", out _), $"{version} {method} failed: {reply}");
        return reply.GetProperty("result").Clone();
    }

    private static string ReadSseData(string body)
    {
        using var reader = new StringReader(body);
        var data = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 && data.Count > 0) break;
            if (line.StartsWith("data:", StringComparison.Ordinal))
                data.Add(line.StartsWith("data: ", StringComparison.Ordinal) ? line[6..] : line[5..]);
        }
        return string.Join('\n', data);
    }

    private static void AssertNames(JsonElement result, string collection, string field, params string[] expected) =>
        Assert.Equal(expected.Order(StringComparer.Ordinal),
            result.GetProperty(collection).EnumerateArray().Select(entry => entry.GetProperty(field).GetString()).Order(StringComparer.Ordinal));

    private static string Text(JsonElement block)
    {
        Assert.Equal("text", block.GetProperty("type").GetString());
        return block.GetProperty("text").GetString()!;
    }

    private static JsonElement SuccessfulDto(JsonElement result)
    {
        Assert.False(result.TryGetProperty("isError", out var error) && error.GetBoolean(), result.GetRawText());
        using var json = JsonDocument.Parse(Text(Assert.Single(result.GetProperty("content").EnumerateArray())));
        if (result.TryGetProperty("structuredContent", out var structured))
            Assert.True(JsonElement.DeepEquals(json.RootElement, structured));
        return json.RootElement.Clone();
    }

    private static Task TraceAsync(StreamWriter trace, object entry, CancellationToken cancellationToken) =>
        trace.WriteLineAsync(JsonSerializer.Serialize(entry).AsMemory(), cancellationToken);
}
