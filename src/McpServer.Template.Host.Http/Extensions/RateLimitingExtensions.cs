using System.Globalization;
using System.Threading.RateLimiting;
using McpServer.Template.Host.Http.Authentication;
using McpServer.Template.Host.Http.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddMcpRateLimiting(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection("RateLimiting"))
            .PostConfigure(options =>
            {
                var rawIdentity = configuration["RateLimiting:Identity"];
                if (!string.IsNullOrEmpty(rawIdentity)
                    && !Enum.TryParse<RateLimitIdentitySource>(rawIdentity, ignoreCase: true, out _))
                {
                    throw new InvalidOperationException(
                        $"Invalid rate limit identity '{rawIdentity}' in configuration. Valid values: clientIp, apiKey.");
                }
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.PolicyName),
                "RateLimiting:PolicyName is required.")
            .Validate(options => options.PermitLimit >= 1,
                "RateLimiting:PermitLimit must be greater than or equal to 1.")
            .Validate(options => options.WindowSeconds >= 1,
                "RateLimiting:WindowSeconds must be greater than or equal to 1.")
            .Validate(options => options.QueueLimit >= 0,
                "RateLimiting:QueueLimit must be greater than or equal to 0.")
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, ConfigureMcpRateLimiterOptions>();

        return services;
    }

    private sealed class ConfigureMcpRateLimiterOptions(
        IOptions<RateLimitingOptions> rateLimitingOptions,
        IOptions<AuthenticationOptions> authenticationOptions)
        : IConfigureOptions<RateLimiterOptions>
    {
        public void Configure(RateLimiterOptions options)
        {
            var rateLimiting = rateLimitingOptions.Value;
            var authOptions = authenticationOptions.Value;

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(rateLimiting.PolicyName, httpContext =>
            {
                var partitionKey = ResolvePartitionKey(httpContext, rateLimiting.Identity, authOptions);

                if (!rateLimiting.Enabled)
                {
                    return RateLimitPartition.GetNoLimiter(partitionKey);
                }

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimiting.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimiting.WindowSeconds),
                    QueueLimit = rateLimiting.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });
            });
        }

        private static string ResolvePartitionKey(
            HttpContext context,
            RateLimitIdentitySource identitySource,
            AuthenticationOptions authenticationOptions)
        {
            if (identitySource == RateLimitIdentitySource.ApiKey)
            {
                var credential = ApiKeyCredentialReader.ExtractCredential(context, authenticationOptions.HeaderName);
                if (!string.IsNullOrWhiteSpace(credential))
                {
                    return $"api-key:{credential}";
                }

                var fallbackIp = context.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrWhiteSpace(fallbackIp))
                {
                    return $"anonymous-ip:{fallbackIp}";
                }

                return "anonymous";
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}