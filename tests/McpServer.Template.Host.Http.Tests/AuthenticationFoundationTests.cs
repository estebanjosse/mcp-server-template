using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using McpServer.Template.Host.Http.Authentication;
using McpServer.Template.Host.Http.Options;

namespace McpServer.Template.Host.Http.Tests;

public sealed class AuthenticationFoundationTests
{
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

    [Fact]
    public void DefaultMode_IsNone_WhenNothingConfigured()
    {
        var options = new AuthenticationOptions();

        options.Mode.Should().Be(AuthenticationMode.None);
    }

    [Fact]
    public void Mode_SelectedFromConfig()
    {
        using var factory = CreateFactory(new()
        {
            ["Authentication:Mode"] = "secure"
        });
        var options = factory.Services
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.Mode.Should().Be(AuthenticationMode.Secure);
    }

    [Fact]
    public void EnvVar_OverridesConfigMode()
    {
        using var factory = CreateFactory(new()
        {
            ["Authentication:Mode"] = "none",
            ["MCP_AUTH_MODE"] = "secure"
        });
        var options = factory.Services
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        options.Mode.Should().Be(AuthenticationMode.Secure);
    }

    [Fact]
    public async Task InvalidMode_InConfig_ThrowsOnStartup()
    {
        await HttpStartup.AssertRejectedAsync("fancy", null,
            "Failed to convert configuration value 'fancy' at 'Authentication:Mode' to type 'McpServer.Template.Host.Http.Options.AuthenticationMode'.");
    }

    [Fact]
    public async Task InvalidMode_InEnvVar_ThrowsOnStartup()
    {
        await HttpStartup.AssertRejectedAsync("none", "unknown",
            "Invalid authentication mode 'unknown' from MCP_AUTH_MODE. Valid modes: none, simple, secure.");
    }

    [Fact]
    public async Task SecureMode_Returns501_WithDescriptiveMessage()
    {
        using var factory = CreateFactory(new()
        {
            ["Authentication:Mode"] = "secure"
        });
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("secure");
        body.Should().Contain("not yet implemented");
    }

    [Fact]
    public async Task NoneMode_AllowsMcpRequests_WithoutCredentials()
    {
        using var factory = CreateFactory(new()
        {
            ["Authentication:Mode"] = "none"
        });
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        var response = await client.SendAsync(request);

        // Should not be 401 — none mode is pass-through
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NoneMode_HealthEndpoint_ReturnsOk()
    {
        using var factory = CreateFactory(new()
        {
            ["Authentication:Mode"] = "none"
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
