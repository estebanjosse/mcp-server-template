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
        using var factory = CreateFactory([]);
        var options = factory.Services
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

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
    public void InvalidMode_InConfig_ThrowsOnStartup()
    {
        var act = () =>
        {
            using var factory = CreateFactory(new()
            {
                ["Authentication:Mode"] = "fancy"
            });
            // Force options resolution to trigger PostConfigure
            _ = factory.Services
                .GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'fancy'*");
    }

    [Fact]
    public void InvalidMode_InEnvVar_ThrowsOnStartup()
    {
        var act = () =>
        {
            using var factory = CreateFactory(new()
            {
                ["MCP_AUTH_MODE"] = "unknown"
            });
            _ = factory.Services
                .GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid authentication mode 'unknown'*MCP_AUTH_MODE*");
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
