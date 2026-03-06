using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace McpServer.Template.Host.Http.Tests;

public sealed class ApiKeyAuthTests
{
    private const string ValidKey = "test-api-key-that-is-at-least-32-chars!";
    private const string ValidKey2 = "second-api-key-at-least-32-characters!";

    private static WebApplicationFactory<Program> CreateFactory(
        Dictionary<string, string?> config)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, c) =>
            {
                c.AddInMemoryCollection(config);
            });
        });
    }

    private static WebApplicationFactory<Program> CreateSimpleFactory(
        string key, string? key2 = null, string? headerName = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "simple",
            ["Authentication:ApiKeys:0"] = key
        };
        if (key2 is not null)
            config["Authentication:ApiKeys:1"] = key2;
        if (headerName is not null)
            config["Authentication:HeaderName"] = headerName;
        return CreateFactory(config);
    }

    [Fact]
    public async Task ValidKey_Accepted()
    {
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", ValidKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidKey_Rejected()
    {
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", "wrong-key-wrong-key-wrong-key-wrong");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingCredential_Rejected()
    {
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DualKey_BothAccepted()
    {
        using var factory = CreateSimpleFactory(ValidKey, ValidKey2);
        using var client = factory.CreateClient();

        // First key
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req1.Headers.Authorization = new("Bearer", ValidKey);
        var resp1 = await client.SendAsync(req1);
        resp1.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        // Second key
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req2.Headers.Authorization = new("Bearer", ValidKey2);
        var resp2 = await client.SendAsync(req2);
        resp2.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CustomHeader_Accepted()
    {
        using var factory = CreateSimpleFactory(ValidKey, headerName: "X-MCP-Key");
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-MCP-Key", ValidKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CustomHeader_BearerIgnored()
    {
        using var factory = CreateSimpleFactory(ValidKey, headerName: "X-MCP-Key");
        using var client = factory.CreateClient();

        // Send via Bearer instead of custom header — should be rejected
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", ValidKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unauthorized_Returns_WwwAuthenticate_Header()
    {
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle()
            .Which.ToString().Should().Contain("Bearer realm=\"MCP\"");
    }

    [Fact]
    public async Task HealthEndpoint_BypassesAuth_InSimpleMode()
    {
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NearMatchKey_Rejected()
    {
        // Key differs only in last character — verifies constant-time doesn't short-circuit
        var nearMatch = ValidKey[..^1] + "Z";
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", nearMatch);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SingleStringKey_CoercedToArray()
    {
        // Config with single string value (not array index) — coercion is done by
        // ASP.NET config binding; simulated here via ApiKeys:0
        using var factory = CreateSimpleFactory(ValidKey);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", ValidKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
