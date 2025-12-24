using McpServer.Template.Mcp.Resources;

namespace McpServer.Template.Mcp.Tests.Resources;

public sealed class WelcomeResourceTests
{
    private readonly WelcomeResource _sut = new();

    [Fact]
    public async Task GetContentAsync_ShouldReturnWelcomeMessage()
    {
        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Welcome to McpServer.Template!");
    }

    [Fact]
    public async Task GetContentAsync_ShouldContainFeaturesList()
    {
        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        result.Should().Contain("Features:");
        result.Should().Contain("Available Tools:");
        result.Should().Contain("Available Prompts:");
        result.Should().Contain("Available Resources:");
    }

    [Fact]
    public async Task GetContentAsync_ShouldMentionEchoTool()
    {
        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        result.Should().Contain("echo");
    }

    [Fact]
    public async Task GetContentAsync_ShouldMentionCalcDivideTool()
    {
        // Act
        var result = await _sut.GetContentAsync();

        // Assert
        result.Should().Contain("calc_divide");
    }
}
