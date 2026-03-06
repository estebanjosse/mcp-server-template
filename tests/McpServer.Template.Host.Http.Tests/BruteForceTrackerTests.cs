using McpServer.Template.Host.Http.Authentication;

namespace McpServer.Template.Host.Http.Tests;

public sealed class BruteForceTrackerTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly BruteForceTracker _tracker;

    public BruteForceTrackerTests()
    {
        _time.SetUtcNow(DateTimeOffset.UtcNow);
        _tracker = new BruteForceTracker(_time);
    }

    [Fact]
    public void NoDelay_UnderThreshold()
    {
        for (var i = 0; i < 5; i++)
        {
            var delay = _tracker.RecordFailure("1.2.3.4");
            delay.Should().BeNull();
        }
    }

    [Fact]
    public void Delay_AfterThresholdExceeded()
    {
        for (var i = 0; i < 5; i++)
            _tracker.RecordFailure("1.2.3.4");

        // 6th failure → first delay
        var delay = _tracker.RecordFailure("1.2.3.4");
        delay.Should().Be(1);
    }

    [Fact]
    public void ProgressiveDelay_Doubles()
    {
        for (var i = 0; i < 5; i++)
            _tracker.RecordFailure("1.2.3.4");

        _tracker.RecordFailure("1.2.3.4").Should().Be(1);  // 6th
        _tracker.RecordFailure("1.2.3.4").Should().Be(2);  // 7th
        _tracker.RecordFailure("1.2.3.4").Should().Be(4);  // 8th
        _tracker.RecordFailure("1.2.3.4").Should().Be(8);  // 9th
        _tracker.RecordFailure("1.2.3.4").Should().Be(16); // 10th
    }

    [Fact]
    public void Delay_CappedAt30Seconds()
    {
        for (var i = 0; i < 20; i++)
            _tracker.RecordFailure("1.2.3.4");

        var delay = _tracker.RecordFailure("1.2.3.4");
        delay.Should().NotBeNull();
        delay!.Value.Should().BeLessThanOrEqualTo(30);
    }

    [Fact]
    public void Success_ResetsCounter()
    {
        for (var i = 0; i < 6; i++)
            _tracker.RecordFailure("1.2.3.4");

        // Should have triggered delay
        _tracker.RecordSuccess("1.2.3.4");

        // After reset, 5 failures should produce no delay
        for (var i = 0; i < 5; i++)
        {
            var delay = _tracker.RecordFailure("1.2.3.4");
            delay.Should().BeNull();
        }
    }

    [Fact]
    public void WindowExpiry_ResetsCounter()
    {
        for (var i = 0; i < 5; i++)
            _tracker.RecordFailure("1.2.3.4");

        // Advance past the 60s window
        _time.Advance(TimeSpan.FromSeconds(61));

        // Should start fresh — no delay
        var delay = _tracker.RecordFailure("1.2.3.4");
        delay.Should().BeNull();
    }

    [Fact]
    public void DifferentIPs_TrackedIndependently()
    {
        for (var i = 0; i < 6; i++)
            _tracker.RecordFailure("1.1.1.1");

        // Different IP should have no delay
        var delay = _tracker.RecordFailure("2.2.2.2");
        delay.Should().BeNull();
    }
}
