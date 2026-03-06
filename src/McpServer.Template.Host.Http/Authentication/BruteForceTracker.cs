using System.Collections.Concurrent;

namespace McpServer.Template.Host.Http.Authentication;

public sealed class BruteForceTracker
{
    private const int FailureThreshold = 5;
    private const int WindowSeconds = 60;
    private const int MaxDelaySeconds = 30;

    private readonly ConcurrentDictionary<string, ClientRecord> _records = new();
    private readonly TimeProvider _timeProvider;

    public BruteForceTracker(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public BruteForceTracker() : this(TimeProvider.System) { }

    public int? RecordFailure(string clientIp)
    {
        var now = _timeProvider.GetUtcNow();
        var record = _records.AddOrUpdate(
            clientIp,
            _ => new ClientRecord(1, now),
            (_, existing) =>
            {
                if ((now - existing.WindowStart).TotalSeconds > WindowSeconds)
                    return new ClientRecord(1, now);

                return existing with { FailureCount = existing.FailureCount + 1 };
            });

        if (record.FailureCount > FailureThreshold)
        {
            var excessFailures = record.FailureCount - FailureThreshold;
            var delay = Math.Min(1 << (excessFailures - 1), MaxDelaySeconds);
            return delay;
        }

        return null;
    }

    public void RecordSuccess(string clientIp)
    {
        _records.TryRemove(clientIp, out _);
    }

    private sealed record ClientRecord(int FailureCount, DateTimeOffset WindowStart);
}
