using McpServer.Template.Infrastructure.Time;

namespace McpServer.Template.Infrastructure.Tests.Time;

public sealed class SystemClockTests
{
    private readonly SystemClock _sut = new();

    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Act
        var result = _sut.UtcNow;

        // Assert
        result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(100));
        result.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Now_ShouldReturnCurrentLocalTime()
    {
        // Act
        var result = _sut.Now;

        // Assert
        result.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMilliseconds(100));
        result.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void UtcNow_CalledMultipleTimes_ShouldReturnIncreasingValues()
    {
        // Act
        var first = _sut.UtcNow;
        Thread.Sleep(10);
        var second = _sut.UtcNow;

        // Assert
        second.Should().BeAfter(first);
    }
}
