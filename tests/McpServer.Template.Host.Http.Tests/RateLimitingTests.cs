using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class RateLimitingTests
{
    private const string ValidKey = "test-api-key-that-is-at-least-32-chars!";
    private const string ValidKey2 = "second-api-key-at-least-32-characters!";

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
    public async Task McpEndpoint_Returns429AndRetryAfter_WhenDefaultLimitExceeded()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:PermitLimit"] = "1",
            ["RateLimiting:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient();

        var firstResponse = await client.SendAsync(CreateMcpRequest());
        var secondResponse = await client.SendAsync(CreateMcpRequest());

        firstResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        secondResponse.Headers.TryGetValues("Retry-After", out var retryAfterValues).Should().BeTrue();
        retryAfterValues.Should().NotBeNull();
        retryAfterValues!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HealthEndpoint_IsNotRateLimited_WhenMcpLimitIsEnabled()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "none",
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:PermitLimit"] = "1",
            ["RateLimiting:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/health");
        var secondResponse = await client.GetAsync("/health");

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiKeyIdentity_PartitionsDifferentClientsIndependently()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "simple",
            ["Authentication:ApiKeys:0"] = ValidKey,
            ["Authentication:ApiKeys:1"] = ValidKey2,
            ["RateLimiting:Enabled"] = "true",
            ["RateLimiting:Identity"] = "apiKey",
            ["RateLimiting:PermitLimit"] = "1",
            ["RateLimiting:WindowSeconds"] = "60"
        });
        using var client = factory.CreateClient();

        var keyOneFirst = CreateMcpRequest();
        keyOneFirst.Headers.Authorization = new("Bearer", ValidKey);
        var keyTwoFirst = CreateMcpRequest();
        keyTwoFirst.Headers.Authorization = new("Bearer", ValidKey2);
        var keyOneSecond = CreateMcpRequest();
        keyOneSecond.Headers.Authorization = new("Bearer", ValidKey);

        var firstResponse = await client.SendAsync(keyOneFirst);
        var secondResponse = await client.SendAsync(keyTwoFirst);
        var thirdResponse = await client.SendAsync(keyOneSecond);

        firstResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        secondResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        thirdResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static HttpRequestMessage CreateMcpRequest()
    {
        return new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }
}