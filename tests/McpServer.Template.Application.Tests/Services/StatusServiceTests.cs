using McpServer.Template.Application.Services;

namespace McpServer.Template.Application.Tests.Services;

public sealed class StatusServiceTests
{
    private readonly StatusService _sut = new();

    [Fact]
    public async Task GetStatusAsync_ShouldReturnStatusWithTimestamp()
    {
        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Server is running at");
        result.Should().Contain("UTC");
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnCurrentTimestamp()
    {
        // Act
        var result = await _sut.GetStatusAsync();
        var now = DateTime.UtcNow;

        // Assert
        result.Should().Contain(now.ToString("yyyy-MM-dd"));
    }
}
