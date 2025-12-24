namespace McpServer.Template.Application.Ports;

public interface IGreetingService
{
    Task<string> GetGreetingAsync(string language, CancellationToken cancellationToken = default);
}
