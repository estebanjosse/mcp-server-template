using McpServer.Abstractions;
using McpServer.Interfaces.Prompts;
using McpServer.Interfaces.Resources;
using McpServer.Interfaces.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Interfaces;

/// <summary>
/// Extension methods for registering tools, prompts, and resources.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all example tools to the service collection.
    /// </summary>
    public static IServiceCollection AddExampleTools(this IServiceCollection services)
    {
        services.AddSingleton<ITool, EchoTool>();
        services.AddSingleton<ITool, CalculatorTool>();
        
        return services;
    }

    /// <summary>
    /// Adds all example prompts to the service collection.
    /// </summary>
    public static IServiceCollection AddExamplePrompts(this IServiceCollection services)
    {
        services.AddSingleton<IPrompt, GreetingPrompt>();
        services.AddSingleton<IPrompt, CodeReviewPrompt>();
        
        return services;
    }

    /// <summary>
    /// Adds all example resources to the service collection.
    /// </summary>
    public static IServiceCollection AddExampleResources(this IServiceCollection services)
    {
        services.AddSingleton<IResource, WelcomeResource>();
        services.AddSingleton<IResource, ServerStatusResource>();
        
        return services;
    }

    /// <summary>
    /// Adds all example implementations (tools, prompts, and resources).
    /// </summary>
    public static IServiceCollection AddAllExamples(this IServiceCollection services)
    {
        return services
            .AddExampleTools()
            .AddExamplePrompts()
            .AddExampleResources();
    }
}
