using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Xunit;

namespace McpServer.Template.Template.Tests;

// Runs outside the generated project, including when it contains no test projects.
internal static class GeneratedStdioProbe
{
    private const string Modern = "2026-07-28";
    private const string HostSuffix = ".Host.Stdio";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private static readonly object ClientInfo = new { name = "generated-stdio-probe", version = "1.0" };

    public static async Task VerifyAsync(string hostDll, string artifacts, bool samples)
    {
        hostDll = Path.GetFullPath(hostDll);
        Assert.True(File.Exists(hostDll), $"Missing generated host: {hostDll}");
        var assembly = Path.GetFileNameWithoutExtension(hostDll);
        Assert.EndsWith(HostSuffix, assembly);
        var output = Path.GetDirectoryName(hostDll)!;
        var cwd = output;
        if (!File.Exists(Path.Combine(output, "appsettings.json")))
        {
            // The plain SDK host does not necessarily copy appsettings.json to TargetPath.
            var project = new DirectoryInfo(output);
            while (project is not null && !File.Exists(Path.Combine(project.FullName, assembly + ".csproj")))
                project = project.Parent;
            cwd = project?.FullName ?? throw new InvalidOperationException($"Cannot locate the configuration root for {hostDll}.");
            Assert.True(File.Exists(Path.Combine(cwd, "appsettings.json")), $"Missing host configuration in {cwd}");
        }

        foreach (var revision in new[] { Modern, "2025-11-25", "2025-06-18" })
            await RunAsync(hostDll, cwd, Path.Combine(artifacts, revision), revision, samples,
                wire => VerifyOperationsAsync(wire, assembly[..^HostSuffix.Length], revision, samples));
    }

    internal static async Task RunAsync(string dll, string cwd, string artifacts, string revision, bool samples,
        Func<Wire, Task> verifyOperations, params string[] arguments)
    {
        Directory.CreateDirectory(artifacts);
        await using var owned = new OwnedProcess(OwnedProcess.Dotnet, [dll, .. arguments]);
        var start = owned.StartInfo;
        start.WorkingDirectory = cwd;
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.StandardInputEncoding = Utf8;
        // The status resource formats UTC using the host's calendar. Scope the Gregorian locale to this child.
        const string locale = "C";
        start.Environment["LC_ALL"] = locale;

        await using var stdout = OpenEvidence("stdout.log");
        await using var stderr = OpenEvidence("stderr.log");
        await using var client = OpenEvidence("client.jsonl");
        await using (var command = OpenEvidence("command.json"))
            await JsonSerializer.SerializeAsync(command, new
            {
                executable = start.FileName, arguments = start.ArgumentList, cwd, revision, samples,
                environmentOverrides = new { LC_ALL = locale }, isolatedProcessGroup = OwnedProcess.SupportsProcessGroups
            });

        using var readers = new CancellationTokenSource();
        owned.Start();
        var process = owned.Process;
        var wire = new Wire(process, stdout, client, readers.Token);
        var stderrDrain = process.StandardError.BaseStream.CopyToAsync(stderr, readers.Token);
        var processExit = owned.RunTargetAsync();
        var completed = Task.WhenAll(processExit, wire.Drain, stderrDrain);
        try
        {
            await verifyOperations(wire);
            Assert.False(processExit.IsCompleted, "The generated host must remain alive until stdin EOF.");
            process.StandardInput.Close();
            try { await completed.WaitAsync(ShutdownTimeout); }
            catch (TimeoutException) when (processExit.IsCompletedSuccessfully && processExit.Result is int exitCode)
            {
                throw new TimeoutException($"Generated host exited with code {exitCode}, but its output pipes did not reach EOF.");
            }
            Assert.Equal(0, await processExit);
            wire.AssertComplete();
            Assert.True(stderr.Length > 0, "Normal host diagnostics must remain on stderr.");
            // EOF proves the pipes closed, not that every process in the session exited.
            await owned.StopAsync();
        }
        catch (Exception probeFailure)
        {
            var stopped = owned.StopAsync();
            Exception? cleanupFailure = null;
            // Let termination close the pipes and preserve trailing evidence. Cancellation is a fallback, not EOF.
            try { await completed.WaitAsync(ShutdownTimeout); }
            catch (Exception failure)
            {
                cleanupFailure = failure;
                if (!completed.IsCompleted)
                {
                    readers.Cancel();
                    try { await completed.WaitAsync(ShutdownTimeout); }
                    catch (OperationCanceledException) when (readers.IsCancellationRequested) { }
                    catch (Exception drainFailure) { cleanupFailure = new AggregateException(cleanupFailure, drainFailure); }
                }
            }
            try { await stopped; }
            catch (Exception failure) when (!ReferenceEquals(failure, probeFailure))
            {
                cleanupFailure = cleanupFailure is null ? failure : new AggregateException(cleanupFailure, failure);
            }
            catch (Exception) { } // The normal-path stop failure is already the primary error.
            throw new InvalidOperationException($"Generated stdio qualification failed for {revision}. Evidence: {artifacts}",
                cleanupFailure is null ? probeFailure : new AggregateException(probeFailure, cleanupFailure));
        }

        FileStream OpenEvidence(string name) => new(Path.Combine(artifacts, name), FileMode.CreateNew,
            FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
    }

    private static async Task VerifyOperationsAsync(Wire wire, string projectName, string revision, bool samples)
    {
        bool modern = revision == Modern;
        var handshake = modern
            ? await CallAsync("server/discover")
            : await wire.RequestAsync("initialize", new { protocolVersion = revision, capabilities = new { }, clientInfo = ClientInfo });
        var server = modern ? handshake.GetProperty("_meta").GetProperty("io.modelcontextprotocol/serverInfo") : handshake.GetProperty("serverInfo");
        Assert.False(string.IsNullOrWhiteSpace(server.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(server.GetProperty("version").GetString()));
        if (modern)
            Assert.Contains(revision, handshake.GetProperty("supportedVersions").EnumerateArray().Select(v => v.GetString()));
        else
        {
            Assert.Equal(revision, handshake.GetProperty("protocolVersion").GetString());
            await wire.NotifyInitializedAsync();
            Assert.Empty((await wire.RequestAsync("ping", new { })).EnumerateObject());
        }

        var capabilities = handshake.GetProperty("capabilities");
        Assert.Equal(JsonValueKind.Object, capabilities.GetProperty("prompts").ValueKind);
        Assert.Equal(JsonValueKind.Object, capabilities.GetProperty("resources").ValueKind);
        Assert.Equal(samples, capabilities.TryGetProperty("tools", out var toolsCapability));
        if (samples)
        {
            Assert.Equal(JsonValueKind.Object, toolsCapability.ValueKind);
            var tools = (await CallAsync("tools/list")).GetProperty("tools").EnumerateArray().ToArray();
            Assert.Equal(new[] { "calc_divide", "echo" }, tools.Select(t => Text(t, "name")).Order(StringComparer.Ordinal));
            AssertToolSchema(tools.Single(t => Text(t, "name") == "echo"), ("message", "string"));
            AssertToolSchema(tools.Single(t => Text(t, "name") == "calc_divide"), ("a", "number"), ("b", "number"));

            const string message = "generated stdio: quote \" slash \\ café ☃ 🚀\nsecond line";
            var before = DateTimeOffset.UtcNow;
            var echo = SuccessDto(await CallAsync("tools/call", new { name = "echo", arguments = new { message } }));
            var after = DateTimeOffset.UtcNow;
            AssertFields(echo, "message", "timestamp");
            Assert.Equal(message, Text(echo, "message"));
            var timestamp = Text(echo, "timestamp");
            Assert.Matches(@"(Z|\+00:00)$", timestamp);
            Assert.InRange(DateTimeOffset.Parse(timestamp, CultureInfo.InvariantCulture), before, after);

            var division = SuccessDto(await CallAsync("tools/call", new { name = "calc_divide", arguments = new { a = 7, b = 2 } }));
            AssertFields(division, "result");
            Assert.Equal(3.5, division.GetProperty("result").GetDouble());

            var zero = await CallAsync("tools/call", new { name = "calc_divide", arguments = new { a = 7, b = 0 } });
            Assert.Equal(JsonValueKind.True, zero.GetProperty("isError").ValueKind);
            Assert.Equal("An error occurred invoking 'calc_divide': Division by zero is not allowed", ToolText(zero));
        }

        var prompts = (await CallAsync("prompts/list")).GetProperty("prompts").EnumerateArray().ToArray();
        AssertUnique(prompts.Select(p => Text(p, "name")));
        var greeting = Assert.Single(prompts, p => Text(p, "name") == "greeting");
        var language = Assert.Single(greeting.GetProperty("arguments").EnumerateArray(), a => Text(a, "name") == "language");
        Assert.True(!language.TryGetProperty("required", out var required) || required.ValueKind == JsonValueKind.False);
        var prompt = await CallAsync("prompts/get", new { name = "greeting", arguments = new { language = "fr" } });
        var promptMessage = Assert.Single(prompt.GetProperty("messages").EnumerateArray());
        Assert.Equal("user", Text(promptMessage, "role"));
        var content = promptMessage.GetProperty("content");
        Assert.Equal("text", Text(content, "type"));
        Assert.Equal("Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.", Text(content, "text"));

        var resources = (await CallAsync("resources/list")).GetProperty("resources").EnumerateArray().ToArray();
        AssertUnique(resources.Select(r => Text(r, "uri")));
        const string welcomeUri = "resource://welcome", statusUri = "resource://status";
        Assert.Equal("text/plain", Text(Assert.Single(resources, r => Text(r, "uri") == welcomeUri), "mimeType"));
        Assert.Equal("application/json", Text(Assert.Single(resources, r => Text(r, "uri") == statusUri), "mimeType"));
        Assert.Equal($"""
            Welcome to {projectName}!

            This is a demonstration of a clean, scalable Model Context Protocol (MCP) server{" "}
            built with .NET 10 and proper architecture.

            Features:
            - Shared business logic between stdio and HTTP transports
            - Strict separation of concerns (Contracts / Application / Infrastructure / MCP Adapter / Hosts)
            - Easy addition of tools, prompts, and resources
            - Testable, idiomatic .NET code with DI and Options pattern

            Available Tools:
            - echo: Echoes back your message with a timestamp
            - calc_divide: Divides two numbers

            Available Prompts:
            - greeting: Generates a multilingual greeting message

            Available Resources:
            - resource://welcome: This welcome message
            - resource://status: Dynamic server status information
            """.ReplaceLineEndings("\n"), (await ReadResourceAsync(welcomeUri, "text/plain")).ReplaceLineEndings("\n"));

        var statusBefore = DateTimeOffset.UtcNow.AddSeconds(-1); // status is truncated to seconds
        using var document = JsonDocument.Parse(await ReadResourceAsync(statusUri, "application/json"));
        var status = document.RootElement;
        AssertFields(status, "Status", "Timestamp", "Uptime", "Version");
        Assert.Equal("Running", Text(status, "Status"));
        Assert.InRange(DateTimeOffset.ParseExact(Text(status, "Timestamp"), "yyyy-MM-dd HH:mm:ss 'UTC'",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal), statusBefore, DateTimeOffset.UtcNow);
        Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", Text(status, "Version"));
        Assert.Matches(@"^(\d+d \d+h \d+m|\d+h \d+m \d+s|\d+m \d+s|\d+s)$", Text(status, "Uptime"));

        const string missingUri = "resource://missing-fixture";
        var error = await wire.RequestAsync("resources/read", Parameters(new { uri = missingUri }), expectError: true);
        Assert.Equal(modern ? -32602 : -32002, error.GetProperty("code").GetInt32());
        Assert.Equal($"Unknown resource URI: '{missingUri}'", Text(error, "message"));
        Assert.False(error.TryGetProperty("data", out _));

        object Parameters(object? parameters)
        {
            parameters ??= new { };
            if (!modern) return parameters;
            var fields = JsonSerializer.SerializeToElement(parameters).EnumerateObject().ToDictionary(p => p.Name, p => (object)p.Value);
            fields.Add("_meta", new Dictionary<string, object>
            {
                ["io.modelcontextprotocol/protocolVersion"] = revision,
                ["io.modelcontextprotocol/clientInfo"] = ClientInfo,
                ["io.modelcontextprotocol/clientCapabilities"] = new { }
            });
            return fields;
        }

        async Task<JsonElement> CallAsync(string method, object? parameters = null)
        {
            var result = await wire.RequestAsync(method, Parameters(parameters));
            Assert.Equal(modern, result.TryGetProperty("resultType", out var type));
            if (modern) Assert.Equal("complete", type.GetString());
            return result;
        }

        async Task<string> ReadResourceAsync(string uri, string mimeType)
        {
            var result = await CallAsync("resources/read", new { uri });
            if (modern)
            {
                Assert.Equal(0, result.GetProperty("ttlMs").GetInt32());
                Assert.Equal("private", Text(result, "cacheScope"));
            }
            else
            {
                Assert.False(result.TryGetProperty("ttlMs", out _));
                Assert.False(result.TryGetProperty("cacheScope", out _));
            }
            var resource = Assert.Single(result.GetProperty("contents").EnumerateArray());
            Assert.Equal(uri, Text(resource, "uri"));
            Assert.Equal(mimeType, Text(resource, "mimeType"));
            return Text(resource, "text");
        }
    }

    private static string Text(JsonElement obj, string name) => obj.GetProperty(name).GetString()
        ?? throw new InvalidDataException($"Expected a string for {name}: {obj}");

    private static void AssertUnique(IEnumerable<string> names)
    {
        var values = names.ToArray();
        Assert.Equal(values.Length, values.Distinct(StringComparer.Ordinal).Count());
    }

    private static void AssertFields(JsonElement obj, params string[] names) => Assert.Equal(
        names.Order(StringComparer.Ordinal), obj.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));

    private static void AssertToolSchema(JsonElement tool, params (string Name, string Type)[] fields)
    {
        var schema = tool.GetProperty("inputSchema");
        Assert.Equal("object", Text(schema, "type"));
        var names = fields.Select(f => f.Name).ToArray();
        Assert.Equal(names.Order(StringComparer.Ordinal), schema.GetProperty("required").EnumerateArray().Select(r => r.GetString()).Order(StringComparer.Ordinal));
        var properties = schema.GetProperty("properties");
        AssertFields(properties, names);
        foreach (var (name, type) in fields) Assert.Equal(type, Text(properties.GetProperty(name), "type"));
    }

    private static string ToolText(JsonElement result)
    {
        var block = Assert.Single(result.GetProperty("content").EnumerateArray());
        Assert.Equal("text", Text(block, "type"));
        return Text(block, "text");
    }

    private static JsonElement SuccessDto(JsonElement result)
    {
        Assert.True(!result.TryGetProperty("isError", out var error) || error.ValueKind == JsonValueKind.False,
            $"Expected a successful tool result, got: {result}");
        using var document = JsonDocument.Parse(ToolText(result));
        if (result.TryGetProperty("structuredContent", out var structured))
            Assert.True(JsonElement.DeepEquals(document.RootElement, structured), "Text and structured tool DTOs must agree.");
        return document.RootElement.Clone();
    }

    internal sealed class Wire
    {
        private const int MaxFrameBytes = 1024 * 1024;
        private readonly Process process;
        private readonly Stream transcript;
        private readonly Channel<JsonElement> replies = Channel.CreateBounded<JsonElement>(new BoundedChannelOptions(8)
        {
            SingleReader = true, SingleWriter = true
        });
        private Exception? stdoutFailure;
        private int nextId;
        private int outstandingId;
        public Task Drain { get; }

        public Wire(Process process, Stream stdout, Stream transcript, CancellationToken cancellation)
        {
            this.process = process;
            this.transcript = transcript;
            Drain = DrainAsync(stdout, cancellation);
        }

        public async Task<JsonElement> RequestAsync(string method, object parameters, bool expectError = false)
        {
            var id = ++nextId;
            using var deadline = new CancellationTokenSource(RequestTimeout);
            try
            {
                Volatile.Write(ref outstandingId, id);
                await SendAsync(new { jsonrpc = "2.0", id, method, @params = parameters }, deadline.Token);
                var frame = await replies.Reader.ReadAsync(deadline.Token);
                Assert.Equal(id, frame.GetProperty("id").GetInt32());
                var isError = frame.TryGetProperty("error", out var error);
                Assert.True(isError == expectError, $"Unexpected response to {method}: {frame}");
                return isError ? error : frame.GetProperty("result");
            }
            catch (OperationCanceledException failure) when (deadline.IsCancellationRequested)
            {
                throw new TimeoutException($"Deadline exceeded for {method} (id {id}).", failure);
            }
            catch (ChannelClosedException failure)
            {
                throw new InvalidDataException($"No valid response to {method} (id {id}).", failure);
            }
        }

        public async Task NotifyInitializedAsync()
        {
            using var deadline = new CancellationTokenSource(RequestTimeout);
            await SendAsync(new { jsonrpc = "2.0", method = "notifications/initialized" }, deadline.Token);
        }

        private async Task SendAsync(object message, CancellationToken cancellation)
        {
            var line = JsonSerializer.Serialize(message) + "\n";
            var bytes = Utf8.GetBytes(line);
            await transcript.WriteAsync(bytes, cancellation);
            await process.StandardInput.BaseStream.WriteAsync(bytes, cancellation);
            await process.StandardInput.BaseStream.FlushAsync(cancellation);
        }

        public void AssertComplete()
        {
            if (stdoutFailure is not null) throw new InvalidDataException("Invalid generated host stdout.", stdoutFailure);
            Assert.False(replies.Reader.TryRead(out var extra), $"Unsolicited or duplicate response: {extra}");
        }

        private async Task DrainAsync(Stream evidence, CancellationToken cancellation)
        {
            var stdout = process.StandardOutput.BaseStream;
            var buffer = new byte[4096];
            using var line = new MemoryStream();
            var lineNumber = 1;
            try
            {
                int count;
                while ((count = await stdout.ReadAsync(buffer, cancellation)) != 0)
                {
                    await evidence.WriteAsync(buffer.AsMemory(0, count), cancellation);
                    // Continue draining raw evidence even after a protocol violation.
                    for (var offset = 0; offset < count && stdoutFailure is null;)
                    {
                        var newline = Array.IndexOf(buffer, (byte)'\n', offset, count - offset);
                        var length = (newline < 0 ? count : newline) - offset;
                        try
                        {
                            RequireFrame(line.Length + length <= MaxFrameBytes);
                            line.Write(buffer, offset, length);
                            if (newline >= 0)
                            {
                                DecodeLine(Utf8.GetString(line.GetBuffer(), 0, (int)line.Length));
                                line.SetLength(0);
                                lineNumber++;
                            }
                        }
                        catch (Exception failure) when (failure is JsonException or InvalidDataException or DecoderFallbackException)
                        {
                            stdoutFailure = new InvalidDataException($"Invalid MCP stdout frame at line {lineNumber}.", failure);
                            replies.Writer.TryComplete(stdoutFailure);
                        }
                        offset = newline < 0 ? count : newline + 1;
                    }
                }
                if (line.Length != 0 && stdoutFailure is null)
                    stdoutFailure = new InvalidDataException($"Unterminated stdout frame at line {lineNumber}.");
            }
            catch (Exception failure)
            {
                replies.Writer.TryComplete(failure);
                throw;
            }
            finally { replies.Writer.TryComplete(stdoutFailure); }
        }

        private void DecodeLine(string line)
        {
            using var document = JsonDocument.Parse(line);
            var frame = document.RootElement;
            RequireFrame(frame.ValueKind == JsonValueKind.Object);
            RequireFrame(frame.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() == frame.EnumerateObject().Count());
            RequireFrame(frame.TryGetProperty("jsonrpc", out var version) && version.ValueKind == JsonValueKind.String && version.GetString() == "2.0");
            var hasId = frame.TryGetProperty("id", out var id);
            var hasResult = frame.TryGetProperty("result", out _);
            var hasError = frame.TryGetProperty("error", out var error);
            if (frame.TryGetProperty("method", out var method))
            {
                // We advertise no client capabilities and expect no server requests.
                RequireFrame(!hasId && !hasResult && !hasError && method.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(method.GetString()));
                RequireFrame(!frame.TryGetProperty("params", out var parameters) || parameters.ValueKind == JsonValueKind.Object);
                return;
            }
            RequireFrame(hasId && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out _) && hasResult != hasError);
            if (hasError)
                RequireFrame(error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var code)
                    && code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out _)
                    && error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String);
            var responseId = id.GetInt32();
            if (responseId <= 0 || Interlocked.CompareExchange(ref outstandingId, 0, responseId) != responseId)
                throw new InvalidDataException($"Unsolicited or duplicate response ID: {responseId}.");
            RequireFrame(replies.Writer.TryWrite(frame.Clone()));
        }

        private static void RequireFrame(bool valid)
        {
            if (!valid) throw new InvalidDataException("Expected a complete, valid MCP JSON-RPC 2.0 frame.");
        }
    }
}
