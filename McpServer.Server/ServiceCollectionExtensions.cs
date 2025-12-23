using System;
using McpServer.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Server;

/// <summary>
/// Extension methods for registering the MCP server in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the MCP server and its configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServer(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options with validation
        services.AddOptions<McpServerOptions>()
            .Bind(configuration.GetSection(McpServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the server implementation
        services.AddSingleton<IMcpServer, McpServerAdapter>();

        return services;
    }

    /// <summary>
    /// Adds the MCP server with custom options to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServer(this IServiceCollection services, Action<McpServerOptions> configureOptions)
    {
        // Configure options with validation
        services.AddOptions<McpServerOptions>()
            .Configure(configureOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the server implementation
        services.AddSingleton<IMcpServer, McpServerAdapter>();

        return services;
    }
}
