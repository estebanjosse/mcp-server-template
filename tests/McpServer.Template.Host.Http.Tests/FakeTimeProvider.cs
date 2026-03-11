namespace McpServer.Template.Host.Http.Tests;

internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;
    public void Advance(TimeSpan duration) => _utcNow += duration;

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
