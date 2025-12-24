using McpServer.Template.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Template.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEchoService, EchoService>();
        services.AddScoped<ICalculatorService, CalculatorService>();
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IGreetingService, GreetingService>();
        
        return services;
    }
}
