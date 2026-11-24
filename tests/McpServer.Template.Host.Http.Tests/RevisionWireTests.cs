using System.Globalization;
using System.Net;
using System.Text.Json;
using McpServer.Template.Infrastructure.Time;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using static McpServer.Template.Host.Http.Tests.McpProtocolBehaviorTests;

namespace McpServer.Template.Host.Http.Tests;

// Wire expectations are pinned to the SDK v2.4.0 StreamableHttpHandler and McpServerImpl.
public sealed class RevisionWireTests : IDisposable
{
    private const string Modern = "2026-07-28";
    private const string MissingUri = "resource://missing-fixture";
    private readonly FixtureClock _clock = new();
    private readonly WebApplicationFactory<Program> _factory;

    public RevisionWireTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authentication:Mode"] = "none" }));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISystemClock>(_clock);
                services.AddMcpServer().WithTools([
                    Tool("fixture_object", () => new { count = 7 }, """{"type":"object","properties":{"count":{"type":"integer"}},"required":["count"]}"""),
                    Tool("fixture_scalar", () => 7, """{"type":"integer"}"""),
                    Tool("fixture_array", () => new[] { 7, 8 }, """{"type":"array","items":{"type":"integer"}}""")]);
            });
        });
    }

    [Theory]
    [InlineData(Modern)]
    [InlineData("2025-11-25")]
    [InlineData("2025-06-18")]
    public async Task Tools_AdvertiseAndReturnRevisionSpecificStructuredShapes(string revision)
    {
        using var client = _factory.CreateClient();
        await InitializeIfLegacy(client, revision);
        var listed = (await Send(client, revision, 2, "tools/list")).GetProperty("result");
        AssertShape(listed, revision, cacheable: true);
        var repeated = (await Send(client, revision, 3, "tools/list")).GetProperty("result");
        AssertJson(repeated, listed.GetRawText());
        var tools = listed.GetProperty("tools").EnumerateArray().ToArray();
        var id = 3;
        foreach (var (name, modernSchema, modernResult) in new[]
        {
            ("fixture_object", """{"type":"object","properties":{"count":{"type":"integer"}},"required":["count"]}""", """{"count":7}"""),
            ("fixture_scalar", """{"type":"integer"}""", "7"),
            ("fixture_array", """{"type":"array","items":{"type":"integer"}}""", "[7,8]")
        })
        {
            var natural = revision == Modern || name == "fixture_object";
            var schema = natural ? modernSchema : $$"""{"type":"object","properties":{"result":{{modernSchema}}},"required":["result"]}""";
            var expected = natural ? modernResult : $$"""{"result":{{modernResult}}}""";
            var tool = tools.Single(entry => entry.GetProperty("name").GetString() == name);
            AssertJson(tool.GetProperty("outputSchema"), schema);
            var result = (await Send(client, revision, ++id, "tools/call", "name", name)).GetProperty("result");
            AssertShape(result, revision, cacheable: false);
            AssertJson(result.GetProperty("structuredContent"), expected);
            if (result.TryGetProperty("isError", out var error)) error.GetBoolean().Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(Modern, -32602)]
    [InlineData("2025-11-25", -32002)]
    [InlineData("2025-06-18", -32002)]
    public async Task Resources_KeepSafeCacheDefaultsAndRevisionSpecificErrors(string revision, int errorCode)
    {
        // Flow the culture of this test's status requests into TestServer only.
        _factory.Server.PreserveExecutionContext = true;
        using var client = _factory.CreateClient();
        await InitializeIfLegacy(client, revision);
        if (revision == Modern)
        {
            var discovery = (await Send(client, revision, 1, "server/discover")).GetProperty("result");
            AssertShape(discovery, revision, cacheable: true);
        }
        var list = (await Send(client, revision, 2, "resources/list")).GetProperty("result");
        AssertShape(list, revision, cacheable: true);
        var repeated = (await Send(client, revision, 3, "resources/list")).GetProperty("result");
        AssertJson(repeated, list.GetRawText());
        var listedUris = list.GetProperty("resources").EnumerateArray().Select(resource => resource.GetProperty("uri").GetString());
        listedUris.Should().BeEquivalentTo("resource://status", "resource://welcome");
        var welcome = (await Send(client, revision, 4, "resources/read", "uri", "resource://welcome")).GetProperty("result");
        AssertShape(welcome, revision, cacheable: true);
        welcome.GetProperty("contents")[0].GetProperty("mimeType").GetString().Should().Be("text/plain");

        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            for (var step = 0; step < 2; step++)
            {
                _clock.UtcNow = new DateTime(2030, 4, 5, 6, 7, 8 + step, DateTimeKind.Utc);
                var status = (await Send(client, revision, 5 + step, "resources/read", "uri", "resource://status")).GetProperty("result");
                AssertShape(status, revision, cacheable: true);
                var content = status.GetProperty("contents")[0];
                content.GetProperty("mimeType").GetString().Should().Be("application/json");
                using var payload = JsonDocument.Parse(content.GetProperty("text").GetString()!);
                payload.RootElement.GetProperty("Status").GetString().Should().Be("Running");
                payload.RootElement.GetProperty("Timestamp").GetString().Should().Be($"2030-04-05 06:07:0{8 + step} UTC");
            }
        }
        finally { CultureInfo.CurrentCulture = previousCulture; }

        var missing = await Send(client, revision, 7, "resources/read", "uri", MissingUri);
        missing.TryGetProperty("result", out _).Should().BeFalse();
        AssertJson(missing.GetProperty("error"), JsonSerializer.Serialize(new { code = errorCode, message = $"Unknown resource URI: '{MissingUri}'" }));
    }

    [Theory]
    [InlineData("protocolVersion", false, "must include '_meta/io.modelcontextprotocol/protocolVersion'.")]
    [InlineData("clientCapabilities", false, "must include '_meta/io.modelcontextprotocol/clientCapabilities' as a JSON object.")]
    [InlineData("clientCapabilities", true, "must include '_meta/io.modelcontextprotocol/clientCapabilities' as a JSON object.")]
    public async Task ModernDiscovery_RejectsMissingOrMalformedMetadata(string field, bool malformed, string diagnostic)
    {
        using var client = _factory.CreateClient();
        var metadata = ModernMetadata();
        var key = $"io.modelcontextprotocol/{field}";
        if (malformed) metadata[key] = "not an object";
        else metadata.Remove(key).Should().BeTrue();
        using var request = CreateRequest(1, "server/discover", new { _meta = metadata }, Modern, "server/discover");
        using var response = await client.SendAsync(request);
        var reply = await ReadReply(response, 1, HttpStatusCode.BadRequest);
        reply.TryGetProperty("result", out _).Should().BeFalse();
        AssertJson(reply.GetProperty("error"), JsonSerializer.Serialize(new
        {
            code = -32602,
            message = $"Requests using protocol version '{Modern}' {diagnostic}"
        }));
    }

    [Theory]
    [InlineData(null, "Missing required Mcp-Name header.")]
    [InlineData("wrong", "Header mismatch: Mcp-Name header value 'wrong' does not match body value 'greeting'.")]
    public async Task ModernPrompt_RejectsMissingOrMismatchedRoutingName(string? header, string message)
    {
        using var client = _factory.CreateClient();
        using var request = CreateRequest(1, "prompts/get", new { name = "greeting", _meta = ModernMetadata() }, Modern, "prompts/get");
        if (header is not null) request.Headers.Add("Mcp-Name", header);
        using var response = await client.SendAsync(request);
        var reply = await ReadReply(response, 1, HttpStatusCode.BadRequest);
        reply.TryGetProperty("result", out _).Should().BeFalse();
        AssertJson(reply.GetProperty("error"), JsonSerializer.Serialize(new { code = -32020, message }));
    }

    private static McpServerTool Tool(string name, Delegate fixture, string schema) => McpServerTool.Create(fixture,
        new McpServerToolCreateOptions { Name = name, UseStructuredContent = true, OutputSchema = JsonSerializer.Deserialize<JsonElement>(schema) });

    private static async Task InitializeIfLegacy(HttpClient client, string revision)
    {
        if (revision == Modern) return;
        using var request = CreateInitializeRequest(revision);
        using var response = await client.SendAsync(request);
        (await ReadReply(response, 1)).GetProperty("result").GetProperty("protocolVersion").GetString().Should().Be(revision);
        using var notification = CreateRequest(null, "notifications/initialized", new { }, revision);
        using var acknowledged = await client.SendAsync(notification);
        acknowledged.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    private static async Task<JsonElement> Send(HttpClient client, string revision, int id, string method, string? key = null, string? value = null)
    {
        var parameters = new Dictionary<string, object>();
        var modern = revision == Modern;
        if (modern) parameters["_meta"] = ModernMetadata();
        if (key is not null) parameters[key] = value!;
        using var request = CreateRequest(id, method, parameters, revision, modern ? method : null);
        if (modern && key is not null) request.Headers.Add("Mcp-Name", value);
        using var response = await client.SendAsync(request);
        response.Headers.Contains("Mcp-Session-Id").Should().BeFalse();
        return await ReadReply(response, id);
    }

    private static void AssertShape(JsonElement result, string revision, bool cacheable)
    {
        var modern = revision == Modern;
        if (modern) result.GetProperty("resultType").GetString().Should().Be("complete");
        else result.TryGetProperty("resultType", out _).Should().BeFalse();
        if (modern && cacheable)
        {
            result.GetProperty("ttlMs").GetInt32().Should().Be(0);
            result.GetProperty("cacheScope").GetString().Should().Be("private");
        }
        else
        {
            result.TryGetProperty("ttlMs", out _).Should().BeFalse();
            result.TryGetProperty("cacheScope", out _).Should().BeFalse();
        }
    }

    private static void AssertJson(JsonElement actual, string expected)
        => JsonElement.DeepEquals(actual, JsonSerializer.Deserialize<JsonElement>(expected)).Should().BeTrue("expected {0}, actual {1}", expected, actual);

    public void Dispose() => _factory.Dispose();

    private sealed class FixtureClock : ISystemClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;
        public DateTime Now => UtcNow.ToLocalTime();
    }
}
