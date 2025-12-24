using McpServer.Template.Infrastructure.AppInfo;
using McpServer.Template.Infrastructure.Time;
using McpServer.Template.Mcp.Resources;
using System.Text.Json;

namespace McpServer.Template.Mcp.Tests.Resources;

public sealed class StatusResourceTests
{
    private readonly ISystemClock _clock;
    private readonly IAppInfoProvider _appInfo;
    private readonly StatusResource _sut;

    public StatusResourceTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _appInfo = Substitute.For<IAppInfoProvider>();
        _sut = new StatusResource(_clock, _appInfo);
    }

    [Fact]
    public async Task GetContentAsync_ShouldReturnJsonWithAllFields()
    {
        // Arrange
        var now = new DateTime(2025, 12, 24, 10, 30, 0, DateTimeKind.Utc);
        _clock.UtcNow.Returns(now);
        _appInfo.Version.Returns("1.0.0");
        _appInfo.Uptime.Returns(TimeSpan.FromMinutes(5));

        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        json.GetProperty("Timestamp").GetString().Should().Contain("2025-12-24");
        json.GetProperty("Version").GetString().Should().Be("1.0.0");
        json.GetProperty("Uptime").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("Status").GetString().Should().Be("Running");
    }

    [Fact]
    public async Task GetContentAsync_ShouldFormatUptimeInDays()
    {
        // Arrange
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _appInfo.Version.Returns("1.0.0");
        _appInfo.Uptime.Returns(TimeSpan.FromDays(2.5));

        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        var uptime = json.GetProperty("Uptime").GetString();
        uptime.Should().Contain("d");
    }

    [Fact]
    public async Task GetContentAsync_ShouldFormatUptimeInMinutes()
    {
        // Arrange
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _appInfo.Version.Returns("1.0.0");
        _appInfo.Uptime.Returns(TimeSpan.FromMinutes(15));

        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        var uptime = json.GetProperty("Uptime").GetString();
        uptime.Should().MatchRegex(@"\d+m \d+s");
    }

    [Fact]
    public async Task GetContentAsync_ShouldFormatUptimeInSeconds()
    {
        // Arrange
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _appInfo.Version.Returns("1.0.0");
        _appInfo.Uptime.Returns(TimeSpan.FromSeconds(30));

        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result);
        var uptime = json.GetProperty("Uptime").GetString();
        uptime.Should().EndWith("s");
    }
}
