using System.Diagnostics;
using System.Reflection;

namespace McpServer.Template.Infrastructure.AppInfo;

public interface IAppInfoProvider
{
    string Version { get; }
    TimeSpan Uptime { get; }
}
