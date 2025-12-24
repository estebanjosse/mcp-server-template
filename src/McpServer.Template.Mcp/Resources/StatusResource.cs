using System.ComponentModel;
using McpServer.Template.Contracts.Constants;
using McpServer.Template.Infrastructure.AppInfo;
using McpServer.Template.Infrastructure.Time;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Resources;

[McpServerResourceType]
public sealed class StatusResource(ISystemClock clock, IAppInfoProvider appInfo)
{
    [McpServerResource(
        UriTemplate = ResourceUris.Status,
        Name = "Server Status",
        MimeType = "application/json")]
    [Description("Dynamic server status with uptime and version")]
    public Task<string> GetContentAsync(CancellationToken cancellationToken = default)
    {
        var status = new
        {
            Timestamp = clock.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
            Version = appInfo.Version,
            Uptime = FormatUptime(appInfo.Uptime),
            Status = "Running"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Task.FromResult(json);
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
        if (uptime.TotalMinutes >= 1)
            return $"{(int)uptime.TotalMinutes}m {uptime.Seconds}s";
        
        return $"{uptime.Seconds}s";
    }
}
