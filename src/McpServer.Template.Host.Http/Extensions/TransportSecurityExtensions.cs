using System.Net;
using McpServer.Template.Host.Http.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Extensions;

public static class TransportSecurityExtensions
{
    public static IServiceCollection AddMcpTransportSecurity(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOptions<TransportSecurityOptions>()
            .Bind(configuration.GetSection("TransportSecurity"))
            .PostConfigure(options =>
            {
                options.KnownProxies = options.KnownProxies
                    .Where(proxy => !string.IsNullOrWhiteSpace(proxy))
                    .ToArray();
            })
            .Validate(options => options.ForwardLimit >= 1,
                "TransportSecurity:ForwardLimit must be greater than or equal to 1.")
            .Validate(options => options.KnownProxies.All(proxy => IPAddress.TryParse(proxy, out _)),
                "TransportSecurity:KnownProxies must contain valid IP addresses.")
            .ValidateOnStart();

        services.AddSingleton<IConfigureOptions<ForwardedHeadersOptions>, ConfigureForwardedHeadersOptions>();

        return services;
    }

    public static WebApplication UseMcpTransportSecurity(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<TransportSecurityOptions>>().Value;

        if (options.ForwardedHeadersEnabled)
        {
            app.UseForwardedHeaders();
        }

        if (HasHttpsEndpointConfigured(app.Configuration))
        {
            app.UseHttpsRedirection();
        }

        return app;
    }

    private static bool HasHttpsEndpointConfigured(IConfiguration configuration)
    {
        var configuredUrls = configuration["ASPNETCORE_URLS"]
            ?? configuration["URLS"]
            ?? configuration["urls"];

        if (!string.IsNullOrWhiteSpace(configuredUrls))
        {
            var urls = configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return configuration
            .GetSection("Kestrel:Endpoints")
            .GetChildren()
            .Select(endpoint => endpoint["Url"])
            .Any(url => !string.IsNullOrWhiteSpace(url)
                && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ConfigureForwardedHeadersOptions(
        IOptions<TransportSecurityOptions> transportSecurityOptions)
        : IConfigureOptions<ForwardedHeadersOptions>
    {
        public void Configure(ForwardedHeadersOptions options)
        {
            var transportSecurity = transportSecurityOptions.Value;

            if (!transportSecurity.ForwardedHeadersEnabled)
            {
                return;
            }

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = transportSecurity.ForwardLimit;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in transportSecurity.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        }
    }
}