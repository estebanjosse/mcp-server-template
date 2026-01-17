using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MetricsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Metrics:Enabled"] = "true"
                });
            });
        });
    }

    [Fact]
    public async Task Metrics_Endpoint_Should_Return_Prometheus_Text()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");

        var payload = await response.Content.ReadAsStringAsync();

        payload.Should().Contain("# HELP");
        payload.Should().Contain("mcp_requests_total");
    }
}
