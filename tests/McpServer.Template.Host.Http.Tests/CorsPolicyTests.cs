using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class CorsPolicyTests
{
    private static WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> config)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, c) =>
            {
                c.AddInMemoryCollection(config);
            });
        });
    }

    [Fact]
    public async Task Preflight_DeniesCrossOriginRequests_ByDefault()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreatePreflightRequest("https://blocked.example"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task Preflight_AllowsConfiguredOrigin()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["Cors:AllowedOrigins:0"] = "https://app.example.com"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreatePreflightRequest("https://app.example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins).Should().BeTrue();
        origins.Should().NotBeNull();
        origins!.Single().Should().Be("https://app.example.com");
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task Preflight_AllowsCredentials_OnlyWhenExplicitlyEnabled()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["Cors:AllowedOrigins:0"] = "https://app.example.com",
            ["Cors:AllowCredentials"] = "true"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreatePreflightRequest("https://app.example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentials).Should().BeTrue();
        credentials.Should().NotBeNull();
        credentials!.Single().Should().Be("true");
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/mcp");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        return request;
    }
}