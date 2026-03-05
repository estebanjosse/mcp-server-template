using McpServer.Template.Host.Http.Authentication;
using McpServer.Template.Host.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Extensions;

public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddMcpAuthentication(
        this IServiceCollection services,
        ConfigurationManager configuration)
    {
        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection("Authentication"))
            .PostConfigure(options =>
            {
                // Validate raw mode value from config (Bind silently defaults invalid enums to 0)
                var rawMode = configuration["Authentication:Mode"];
                if (!string.IsNullOrEmpty(rawMode)
                    && !Enum.TryParse<AuthenticationMode>(rawMode, ignoreCase: true, out _))
                {
                    throw new InvalidOperationException(
                        $"Invalid authentication mode '{rawMode}' in configuration. Valid modes: none, simple, secure.");
                }

                var modeOverride = configuration["MCP_AUTH_MODE"];
                if (!string.IsNullOrEmpty(modeOverride))
                {
                    if (Enum.TryParse<AuthenticationMode>(modeOverride, ignoreCase: true, out var mode))
                    {
                        options.Mode = mode;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Invalid authentication mode '{modeOverride}' from MCP_AUTH_MODE. Valid modes: none, simple, secure.");
                    }
                }

                var keyOverride = configuration["MCP_AUTH_API_KEY"];
                if (!string.IsNullOrEmpty(keyOverride))
                {
                    options.ApiKeys = [keyOverride];
                }

                var headerOverride = configuration["MCP_AUTH_HEADER"];
                if (!string.IsNullOrEmpty(headerOverride))
                {
                    options.HeaderName = headerOverride;
                }

                // Discard empty/whitespace-only keys
                options.ApiKeys = options.ApiKeys
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .ToArray();
            })
            .Validate(options =>
            {
                if (options.Mode == AuthenticationMode.Simple)
                {
                    if (options.ApiKeys.Length == 0)
                        return false;

                    foreach (var key in options.ApiKeys)
                    {
                        if (key.Length < 32)
                            return false;
                    }
                }

                if (options.HeaderName is not null)
                {
                    if (string.IsNullOrWhiteSpace(options.HeaderName)
                        || options.HeaderName.Any(c => char.IsControl(c) || c == ':' || c == ' '))
                        return false;
                }

                return true;
            }, "Authentication configuration is invalid. " +
               "In simple mode, at least one API key of minimum 32 characters is required. " +
               "Header names must not be empty or contain control characters, colons, or spaces.")
            .ValidateOnStart();

        services.AddSingleton<NoneAuthStrategy>();
        services.AddSingleton<SecurePlaceholderStrategy>();

        services.AddSingleton<IMcpAuthStrategy>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
            return options.Mode switch
            {
                AuthenticationMode.None => sp.GetRequiredService<NoneAuthStrategy>(),
                AuthenticationMode.Secure => sp.GetRequiredService<SecurePlaceholderStrategy>(),
                _ => throw new InvalidOperationException(
                    $"No authentication strategy registered for mode '{options.Mode}'.")
            };
        });

        return services;
    }
}
