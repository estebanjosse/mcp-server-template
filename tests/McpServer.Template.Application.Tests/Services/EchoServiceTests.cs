using McpServer.Template.Application.Services;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Tests.Services;

public sealed class EchoServiceTests
{
    private readonly EchoService _sut = new();

    [Fact]
    public async Task EchoAsync_ShouldReturnMessageWithTimestamp()
    {
        // Arrange
        var request = new EchoRequest("Hello World");

        // Act
        var result = await _sut.EchoAsync(request);

        // Assert
        result.Message.Should().Be("Hello World");
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test message")]
    [InlineData("Special characters: @#$%^&*()")]
    public async Task EchoAsync_ShouldHandleVariousMessages(string message)
    {
        // Arrange
        var request = new EchoRequest(message);

        // Act
        var result = await _sut.EchoAsync(request);

        // Assert
        result.Message.Should().Be(message);
    }
}
