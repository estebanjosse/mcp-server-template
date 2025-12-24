using McpServer.Template.Infrastructure.AppInfo;

namespace McpServer.Template.Infrastructure.Tests.AppInfo;

public sealed class AppInfoProviderTests
{
    [Fact]
    public void Version_ShouldReturnNonEmptyString()
    {
        // Arrange
        var sut = new AppInfoProvider();

        // Act
        var result = sut.Version;

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Uptime_ShouldReturnPositiveTimeSpan()
    {
        // Arrange
        var sut = new AppInfoProvider();
        Thread.Sleep(10);

        // Act
        var result = sut.Uptime;

        // Assert
        result.Should().BePositive();
    }

    [Fact]
    public void Uptime_ShouldIncreaseOverTime()
    {
        // Arrange
        var sut = new AppInfoProvider();
        var first = sut.Uptime;
        Thread.Sleep(50);

        // Act
        var second = sut.Uptime;

        // Assert
        second.Should().BeGreaterThan(first);
    }

    [Fact]
    public void Uptime_ShouldBeRealisticValue()
    {
        // Arrange
        var sut = new AppInfoProvider();

        // Act
        var result = sut.Uptime;

        // Assert
        result.TotalSeconds.Should().BeGreaterThanOrEqualTo(0);
        result.TotalDays.Should().BeLessThan(365); // Reasonable upper bound
    }
}
