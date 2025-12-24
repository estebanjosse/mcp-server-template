namespace McpServer.Template.Application.Services;

public sealed class StatusService : IStatusService
{
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = $"Server is running at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        return Task.FromResult(status);
    }
}
