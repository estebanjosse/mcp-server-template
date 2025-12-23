using System;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Examples.Resources;

/// <summary>
/// A dynamic resource that provides server status information.
/// </summary>
public class ServerStatusResource : IResource
{
    public string Uri => "resource://server-status";

    public string Name => "Server Status";

    public string? Description => "Current server status and statistics";

    public string MimeType => "application/json";

    public Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        var status = new
        {
            status = "running",
            timestamp = DateTime.UtcNow.ToString("O"),
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"dd\.hh\:mm\:ss"),
            version = "1.0.0",
            platform = Environment.OSVersion.Platform.ToString(),
            dotnetVersion = Environment.Version.ToString()
        };

        var json = System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Task.FromResult(json);
    }
}
