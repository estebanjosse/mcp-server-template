namespace McpServer.Template.Application.Services;

public interface IGreetingService
{
    Task<string> GetGreetingAsync(string language, CancellationToken cancellationToken = default);
}
