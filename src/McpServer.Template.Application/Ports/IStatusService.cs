namespace McpServer.Template.Application.Ports;

public interface IStatusService
{
    Task<string> GetStatusAsync(CancellationToken cancellationToken = default);
}
