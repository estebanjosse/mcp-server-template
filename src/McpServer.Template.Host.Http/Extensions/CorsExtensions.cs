using McpServer.Template.Host.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddMcpCors(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOptions<CorsOptions>()
            .Bind(configuration.GetSection("Cors"))
            .PostConfigure(options =>
            {
                options.AllowedOrigins = options.AllowedOrigins
                    .Where(origin => !string.IsNullOrWhiteSpace(origin))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.PolicyName),
                "Cors:PolicyName is required.")
            .Validate(options => options.AllowedOrigins.All(origin => Uri.TryCreate(origin, UriKind.Absolute, out _)),
                "Cors:AllowedOrigins must contain absolute origins.")
            .Validate(options => !options.AllowCredentials || options.AllowedOrigins.Length > 0,
                "Cors:AllowCredentials requires at least one allowed origin.")
            .ValidateOnStart();

        services.AddCors();
        services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>, ConfigureMcpCorsOptions>();

        return services;
    }

    private sealed class ConfigureMcpCorsOptions(
        IOptions<CorsOptions> corsOptions)
        : IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>
    {
        public void Configure(Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions options)
        {
            var cors = corsOptions.Value;

            options.AddPolicy(cors.PolicyName, policy =>
            {
                policy.WithMethods("GET", "POST", "OPTIONS");
                policy.AllowAnyHeader();

                if (cors.AllowedOrigins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy.WithOrigins(cors.AllowedOrigins);

                if (cors.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        }
    }
}