using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace McpServer.Template.Host.Http.Tests;

public sealed class McpProtocolBehaviorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public McpProtocolBehaviorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task McpEndpoint_WhenProtocolVersionHeaderIsInvalid_ShouldReturnBadRequest()
    {
        using var client = _factory.CreateClient();
        using var request = CreateJsonRequest(InitializeRequestBody());
        request.Headers.Add("MCP-Protocol-Version", "invalid-version");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task McpEndpoint_WhenProtocolVersionHeaderIsMissing_InitializeShouldNotReturnBadRequest()
    {
        using var client = _factory.CreateClient();
        using var request = CreateJsonRequest(InitializeRequestBody());

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task McpEndpoint_WhenNonInitializeRequestHasNoSessionId_ShouldBeRejected()
    {
        using var client = _factory.CreateClient();
        using var request = CreateJsonRequest("""
            {
              "jsonrpc": "2.0",
              "id": "2",
              "method": "tools/list",
              "params": {}
            }
            """);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotAcceptable);
    }

    private static HttpRequestMessage CreateJsonRequest(string jsonBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        return request;
    }

    private static string InitializeRequestBody() =>
        """
        {
          "jsonrpc": "2.0",
          "id": "1",
          "method": "initialize",
          "params": {
            "protocolVersion": "2025-06-18",
            "capabilities": {},
            "clientInfo": {
              "name": "tests",
              "version": "1.0.0"
            }
          }
        }
        """;
}
