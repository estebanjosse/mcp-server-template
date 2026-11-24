using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Template.Host.Http.Tests;

public sealed class AuthContinuityTests
{
    private const string ModernVersion = "2026-07-28";
    private const string BearerHeader = "Authorization";
    private const string CustomHeader = "X-MCP-Key";
    private const string ValidKey = "auth-continuity-tests-not-a-production-key";
    private static readonly object ClientInfo = new { name = "auth-continuity-tests", version = "1.0" };

    [Theory]
    [InlineData(ModernVersion, null)]
    [InlineData(ModernVersion, BearerHeader)]
    [InlineData(ModernVersion, CustomHeader)]
    [InlineData("2025-11-25", null)]
    [InlineData("2025-11-25", BearerHeader)]
    [InlineData("2025-11-25", CustomHeader)]
    [InlineData("2025-06-18", null)]
    [InlineData("2025-06-18", BearerHeader)]
    [InlineData("2025-06-18", CustomHeader)]
    public async Task McpRequests_PreserveAuthenticationAcrossRevisions(string version, string? credentialHeader)
    {
        var config = new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = credentialHeader is null ? "none" : "simple",
            ["Authentication:ApiKeys:0"] = ValidKey,
            ["Authentication:HeaderName"] = credentialHeader == CustomHeader ? CustomHeader : null,
            ["MCP_AUTH_MODE"] = null,
            ["MCP_AUTH_API_KEY"] = null,
            ["MCP_AUTH_HEADER"] = null
        };
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(config)));
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        // Use the bound endpoint rather than the factory client's default localhost URI.
        var server = factory.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            UseProxy = false
        }) { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };

        var modern = version == ModernVersion;
        if (!modern)
        {
            using var initialize = CreateRequest(1, "initialize", new
            {
                protocolVersion = version,
                capabilities = new { },
                clientInfo = ClientInfo
            });
            AddCredential(initialize, credentialHeader, ValidKey);
            using var initialized = await client.SendAsync(initialize);
            var handshake = await ReadAcceptedResult(initialized, 1);
            handshake.GetProperty("protocolVersion").GetString().Should().Be(version);

            using var notification = CreateRequest(null, "notifications/initialized", new { }, version);
            AddCredential(notification, credentialHeader, ValidKey);
            using var acknowledged = await client.SendAsync(notification);
            acknowledged.StatusCode.Should().Be(HttpStatusCode.Accepted);
            AssertNoChallengeOrSession(acknowledged);
        }

        // The modern flow has no handshake. Legacy initialization must not authorize later requests.
        if (credentialHeader is not null)
        {
            await AssertRejected(2, null, ValidKey);
            await AssertRejected(3, credentialHeader, ValidKey[..^1] + "Z");
            await AssertRejected(4, credentialHeader == BearerHeader ? CustomHeader : BearerHeader, ValidKey);
        }

        using var request = CreatePromptRequest(5, version);
        AddCredential(request, credentialHeader, ValidKey);
        using var response = await client.SendAsync(request);
        var result = await ReadAcceptedResult(response, 5);
        if (modern)
            result.GetProperty("resultType").GetString().Should().Be("complete");
        else
            result.TryGetProperty("resultType", out _).Should().BeFalse();
        var message = result.GetProperty("messages").EnumerateArray().Should().ContainSingle().Subject;
        message.GetProperty("content").GetProperty("text").GetString().Should().Be(
            "Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.");

        async Task AssertRejected(int id, string? header, string key)
        {
            using var deniedRequest = CreatePromptRequest(id, version);
            AddCredential(deniedRequest, header, key);
            using var denied = await client.SendAsync(deniedRequest);
            denied.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            denied.Headers.GetValues("WWW-Authenticate").Should().Equal("Bearer realm=\"MCP\"");
            denied.Headers.Contains("Mcp-Session-Id").Should().BeFalse();
            (await denied.Content.ReadAsStringAsync()).Should().BeEmpty("the guard rejects before JSON-RPC dispatch");
        }
    }

    private static HttpRequestMessage CreatePromptRequest(int id, string version)
    {
        const string method = "prompts/get";
        const string name = "greeting";
        var parameters = new Dictionary<string, object> { ["name"] = name, ["arguments"] = new { language = "fr" } };
        var modern = version == ModernVersion;
        if (modern)
        {
            parameters["_meta"] = new Dictionary<string, object>
            {
                ["io.modelcontextprotocol/protocolVersion"] = version,
                ["io.modelcontextprotocol/clientInfo"] = ClientInfo,
                ["io.modelcontextprotocol/clientCapabilities"] = new { }
            };
        }
        var request = CreateRequest(id, method, parameters, version);
        if (modern)
        {
            request.Headers.Add("Mcp-Method", method);
            request.Headers.Add("Mcp-Name", name);
        }
        return request;
    }

    private static HttpRequestMessage CreateRequest(int? id, string method, object parameters, string? version = null)
    {
        var message = new Dictionary<string, object> { ["jsonrpc"] = "2.0", ["method"] = method, ["params"] = parameters };
        if (id is { } requestId) message["id"] = requestId;
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = JsonContent.Create(message) };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (version is not null) request.Headers.Add("Mcp-Protocol-Version", version);
        return request;
    }

    private static void AddCredential(HttpRequestMessage request, string? header, string key)
    {
        if (header == BearerHeader) request.Headers.Authorization = new("Bearer", key);
        else if (header is not null) request.Headers.Add(header, key);
    }

    private static async Task<JsonElement> ReadAcceptedResult(HttpResponseMessage response, int id)
    {
        var reply = await McpProtocolBehaviorTests.ReadReply(response, id);
        AssertNoChallengeOrSession(response);
        reply.TryGetProperty("error", out _).Should().BeFalse();
        return reply.GetProperty("result");
    }

    private static void AssertNoChallengeOrSession(HttpResponseMessage response)
    {
        response.Headers.Contains("WWW-Authenticate").Should().BeFalse();
        response.Headers.Contains("Mcp-Session-Id").Should().BeFalse();
    }
}
