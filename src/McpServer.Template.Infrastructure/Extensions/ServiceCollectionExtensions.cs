using McpServer.Template.Infrastructure.AppInfo;
using McpServer.Template.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Template.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IAppInfoProvider, AppInfoProvider>();
        
        return services;
    }
}
