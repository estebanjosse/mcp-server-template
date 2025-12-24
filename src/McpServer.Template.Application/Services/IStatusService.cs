namespace McpServer.Template.Application.Services;

public interface IStatusService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
