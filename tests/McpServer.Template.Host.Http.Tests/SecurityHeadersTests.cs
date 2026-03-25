using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class SecurityHeadersTests
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
    public async Task DefaultHeaders_AreAdded_ToMcpResponses()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreateMcpRequest());

        response.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).Should().BeTrue();
        nosniff.Should().NotBeNull();
        nosniff!.Single().Should().Be("nosniff");

        response.Headers.TryGetValues("X-Frame-Options", out var frameOptions).Should().BeTrue();
        frameOptions.Should().NotBeNull();
        frameOptions!.Single().Should().Be("DENY");

        response.Headers.Contains("Content-Security-Policy").Should().BeFalse();
    }

    [Fact]
    public async Task Hsts_IsAdded_OnHttpsResponses()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none"
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Strict-Transport-Security", out var hsts).Should().BeTrue();
        hsts.Should().NotBeNull();
        hsts!.Single().Should().Be("max-age=31536000; includeSubDomains");
    }

    [Fact]
    public async Task ContentSecurityPolicy_IsAdded_WhenConfigured()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["SecurityHeaders:ContentSecurityPolicy"] = "default-src 'none'"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreateMcpRequest());

        response.Headers.TryGetValues("Content-Security-Policy", out var csp).Should().BeTrue();
        csp.Should().NotBeNull();
        csp!.Single().Should().Be("default-src 'none'");
    }

    [Fact]
    public async Task XFrameOptions_CanBeDisabled()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["SecurityHeaders:XFrameOptionsEnabled"] = "false"
        });
        using var client = factory.CreateClient();

        var response = await client.SendAsync(CreateMcpRequest());

        response.Headers.Contains("X-Frame-Options").Should().BeFalse();
    }

    private static HttpRequestMessage CreateMcpRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}