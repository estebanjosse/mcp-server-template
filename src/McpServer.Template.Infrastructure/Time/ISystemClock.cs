namespace McpServer.Template.Infrastructure.Time;

public interface ISystemClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
