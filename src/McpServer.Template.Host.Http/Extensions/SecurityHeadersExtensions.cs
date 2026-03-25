using McpServer.Template.Host.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Extensions;

public static class SecurityHeadersExtensions
{
    public static IServiceCollection AddMcpSecurityHeaders(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOptions<SecurityHeadersOptions>()
            .Bind(configuration.GetSection("SecurityHeaders"))
            .PostConfigure(options =>
            {
                options.XFrameOptionsValue = options.XFrameOptionsValue.Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(options.ContentSecurityPolicy))
                {
                    options.ContentSecurityPolicy = null;
                }
            })
            .Validate(options => options.HstsMaxAgeSeconds >= 0,
                "SecurityHeaders:HstsMaxAgeSeconds must be greater than or equal to 0.")
            .Validate(options => !options.XFrameOptionsEnabled
                || options.XFrameOptionsValue is "DENY" or "SAMEORIGIN",
                "SecurityHeaders:XFrameOptionsValue must be DENY or SAMEORIGIN when X-Frame-Options is enabled.")
            .ValidateOnStart();

        return services;
    }

    public static WebApplication UseMcpSecurityHeaders(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;

        if (!options.Enabled)
        {
            return app;
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                ApplyHeaders(context, options);
                return Task.CompletedTask;
            });

            await next();
        });

        return app;
    }

    private static void ApplyHeaders(HttpContext context, SecurityHeadersOptions options)
    {
        var headers = context.Response.Headers;

        if (options.HstsEnabled && context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = BuildHstsValue(options);
        }

        if (options.XContentTypeOptionsEnabled)
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        if (options.XFrameOptionsEnabled)
        {
            headers["X-Frame-Options"] = options.XFrameOptionsValue;
        }

        if (options.ContentSecurityPolicy is not null)
        {
            headers["Content-Security-Policy"] = options.ContentSecurityPolicy;
        }
    }

    private static string BuildHstsValue(SecurityHeadersOptions options)
    {
        var value = $"max-age={options.HstsMaxAgeSeconds}";

        if (options.HstsIncludeSubDomains)
        {
            value += "; includeSubDomains";
        }

        if (options.HstsPreload)
        {
            value += "; preload";
        }

        return value;
    }
}