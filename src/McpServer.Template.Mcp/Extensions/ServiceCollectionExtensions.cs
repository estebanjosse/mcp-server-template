using McpServer.Template.Application.Extensions;
using McpServer.Template.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpTemplateModules(this IServiceCollection services)
    {
        // Add business logic layers
        services.AddApplication();
        services.AddInfrastructure();

        // Register MCP components from this assembly
        services.AddMcpServer()
            .WithToolsFromAssembly()
            .WithPromptsFromAssembly()
            .WithResourcesFromAssembly();

        return services;
    }
}
