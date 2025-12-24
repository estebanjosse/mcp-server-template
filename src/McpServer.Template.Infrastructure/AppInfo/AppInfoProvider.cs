using System.Diagnostics;
using System.Reflection;

namespace McpServer.Template.Infrastructure.AppInfo;

public sealed class AppInfoProvider : IAppInfoProvider
{
    private readonly DateTime _startTime;

    public AppInfoProvider()
    {
        _startTime = DateTime.UtcNow;
    }

    public string Version => 
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

    public TimeSpan Uptime => DateTime.UtcNow - _startTime;
}
