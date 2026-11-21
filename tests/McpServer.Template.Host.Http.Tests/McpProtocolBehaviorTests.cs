using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class McpProtocolBehaviorTests : IDisposable
{
    private const string ModernVersion = "2026-07-28";
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none"
        })));

    [Theory]
    [InlineData("2025-06-18")]
    [InlineData("2025-11-25")]
    public async Task Initialize_OnHistoricalRevision_NegotiatesExactVersionWithoutSession(string version)
    {
        using var client = _factory.CreateClient();
        using var initialize = CreateInitializeRequest(version);
        using var initialized = await client.SendAsync(initialize);
        var result = (await ReadReply(initialized, 1)).GetProperty("result");
        result.GetProperty("protocolVersion").GetString().Should().Be(version);
        initialized.Headers.Contains("Mcp-Session-Id").Should().BeFalse("legacy interoperability must not use persistent sessions");
        using var notification = CreateRequest(null, "notifications/initialized", new { }, version);
        using var acknowledged = await client.SendAsync(notification);
        acknowledged.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var list = CreateRequest(2, "prompts/list", new { }, version);
        using var listed = await client.SendAsync(list);
        var prompts = (await ReadReply(listed, 2)).GetProperty("result");
        prompts.TryGetProperty("resultType", out _).Should().BeFalse("legacy replies must keep their legacy shape");
        PromptNames(prompts).Should().Contain("greeting");
    }

    [Fact]
    public async Task Discover_OnModernRevision_AdvertisesModernVersionWithoutSession()
    {
        using var client = _factory.CreateClient();
        using var request = CreateRequest(1, "server/discover", new { _meta = ModernMetadata() }, ModernVersion, "server/discover");
        using var response = await client.SendAsync(request);
        var result = (await ReadReply(response, 1)).GetProperty("result");
        result.GetProperty("supportedVersions").EnumerateArray().Select(v => v.GetString()).Should().Contain(ModernVersion);
        result.GetProperty("capabilities").GetProperty("prompts").ValueKind.Should().Be(JsonValueKind.Object);
        response.Headers.Contains("Mcp-Session-Id").Should().BeFalse();
    }

    [Fact]
    public async Task ListPrompts_OnModernRevision_WorksWithoutPriorHandshake()
    {
        using var client = _factory.CreateClient();
        using var request = CreateRequest(1, "prompts/list", new { _meta = ModernMetadata() }, ModernVersion, "prompts/list");
        using var response = await client.SendAsync(request);
        var result = (await ReadReply(response, 1)).GetProperty("result");
        result.GetProperty("resultType").GetString().Should().Be("complete");
        PromptNames(result).Should().Contain("greeting");
        response.Headers.Contains("Mcp-Session-Id").Should().BeFalse();
    }

    [Fact]
    public async Task Discover_WithUnsupportedVersion_ReturnsSupportedModernRevisions()
    {
        const string unsupportedVersion = "2099-12-31";
        using var client = _factory.CreateClient();
        using var request = CreateRequest(1, "server/discover", new { _meta = ModernMetadata() }, unsupportedVersion, "server/discover");
        using var response = await client.SendAsync(request);
        await AssertUnsupportedVersion(response, unsupportedVersion);
    }

    [Fact]
    public async Task Initialize_WithMalformedVersionHeader_ReturnsUnsupportedVersion()
    {
        const string malformedVersion = "invalid-version";
        using var client = _factory.CreateClient();
        using var request = CreateInitializeRequest("2025-06-18", malformedVersion);
        using var response = await client.SendAsync(request);
        await AssertUnsupportedVersion(response, malformedVersion);
    }

    [Fact]
    public async Task Discover_WithMismatchedMethodHeader_ReturnsCorrelatedHeaderMismatch()
    {
        using var client = _factory.CreateClient();
        using var request = CreateRequest(1, "server/discover", new { _meta = ModernMetadata() }, ModernVersion, "prompts/list");
        using var response = await client.SendAsync(request);
        var body = await ReadReply(response, 1, HttpStatusCode.BadRequest);
        body.TryGetProperty("result", out _).Should().BeFalse();
        var error = body.GetProperty("error");
        error.GetProperty("code").GetInt32().Should().Be(-32020);
        error.GetProperty("message").GetString().Should().Be("Header mismatch: Mcp-Method header value 'prompts/list' does not match body value 'server/discover'.");
    }

    [Theory]
    [InlineData("GET", ModernVersion)]
    [InlineData("DELETE", ModernVersion)]
    [InlineData("GET", "2025-11-25")]
    [InlineData("DELETE", "2025-11-25")]
    public async Task McpEndpoint_OnStatelessHost_AllowsOnlyPost(string method, string version)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/mcp");
        request.Headers.Add("MCP-Protocol-Version", version);
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.Allow.Should().Equal("POST");
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty("stateless endpoint routing does not map GET or DELETE");
    }

    private static HttpRequestMessage CreateInitializeRequest(string version, string? header = null) =>
        CreateRequest(1, "initialize", new
        {
            protocolVersion = version,
            capabilities = new { },
            clientInfo = new { name = "template-tests", version = "1.0" }
        }, header);

    private static async Task AssertUnsupportedVersion(HttpResponseMessage response, string version)
    {
        var body = await ReadReply(response, 1, HttpStatusCode.BadRequest);
        body.TryGetProperty("result", out _).Should().BeFalse();
        var error = body.GetProperty("error");
        error.GetProperty("code").GetInt32().Should().Be(-32022);
        error.GetProperty("message").GetString().Should().Be($"Bad Request: The MCP-Protocol-Version header value '{version}' is not supported.");
        var data = error.GetProperty("data");
        data.GetProperty("requested").GetString().Should().Be(version);
        data.GetProperty("supported").EnumerateArray().Select(v => v.GetString()).Should().Equal(ModernVersion);
    }

    private static Dictionary<string, object> ModernMetadata() => new()
    {
        ["io.modelcontextprotocol/protocolVersion"] = ModernVersion,
        ["io.modelcontextprotocol/clientInfo"] = new { name = "template-tests", version = "1.0" },
        ["io.modelcontextprotocol/clientCapabilities"] = new { }
    };

    private static IEnumerable<string?> PromptNames(JsonElement result) =>
        result.GetProperty("prompts").EnumerateArray().Select(p => p.GetProperty("name").GetString());

    private static HttpRequestMessage CreateRequest(int? id, string method, object parameters, string? version = null, string? methodHeader = null)
    {
        var message = new Dictionary<string, object> { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = parameters };
        if (id is { } requestId) message["id"] = requestId;
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = JsonContent.Create(message) };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (version is not null) request.Headers.Add("MCP-Protocol-Version", version);
        if (methodHeader is not null) request.Headers.Add("Mcp-Method", methodHeader);
        return request;
    }

    private static async Task<JsonElement> ReadReply(HttpResponseMessage response, int id, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var text = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expected, $"the MCP reply for request {id} was {text}");
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.Should().BeOneOf("application/json", "text/event-stream");
        if (mediaType == "text/event-stream")
        {
            using var reader = new StringReader(text);
            var data = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                if (line.Length == 0 && data.Count > 0) break;
                if (line.StartsWith("data:", StringComparison.Ordinal)) data.Add(line[5..].TrimStart());
            }
            text = string.Join('\n', data);
        }
        using var json = JsonDocument.Parse(text);
        var reply = json.RootElement;
        reply.GetProperty("jsonrpc").GetString().Should().Be("2.0");
        var responseId = reply.GetProperty("id");
        responseId.ValueKind.Should().Be(JsonValueKind.Number, $"the reply must correlate to request {id}: {text}");
        responseId.GetInt32().Should().Be(id);
        return reply.Clone();
    }

    public void Dispose() => _factory.Dispose();
}
