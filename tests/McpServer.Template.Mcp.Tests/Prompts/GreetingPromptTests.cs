using McpServer.Template.Application.Ports;
using McpServer.Template.Mcp.Prompts;

namespace McpServer.Template.Mcp.Tests.Prompts;

public sealed class GreetingPromptTests
{
    private readonly IGreetingService _greetingService;
    private readonly GreetingPrompt _sut;

    public GreetingPromptTests()
    {
        _greetingService = Substitute.For<IGreetingService>();
        _sut = new GreetingPrompt(_greetingService);
    }

    [Theory]
    [InlineData("en", "Hello! Welcome to the MCP Server Template.")]
    [InlineData("fr", "Bonjour ! Bienvenue sur le modèle de serveur MCP.")]
    public async Task GetPromptAsync_ShouldReturnGreetingWithInstructions(string language, string greeting)
    {
        // Arrange
        _greetingService.GetGreetingAsync(language, Arg.Any<CancellationToken>())
            .Returns(greeting);

        // Act
        var result = await _sut.GetPromptAsync(language);

        // Assert
        result.Should().Contain(greeting);
        result.Should().Contain("You can use the available tools");
    }

    [Fact]
    public async Task GetPromptAsync_WithoutLanguage_ShouldUseEnglishDefault()
    {
        // Arrange
        const string defaultGreeting = "Hello! Welcome to the MCP Server Template.";
        _greetingService.GetGreetingAsync("en", Arg.Any<CancellationToken>())
            .Returns(defaultGreeting);

        // Act
        var result = await _sut.GetPromptAsync();

        // Assert
        await _greetingService.Received(1).GetGreetingAsync("en", Arg.Any<CancellationToken>());
        result.Should().Contain(defaultGreeting);
    }
}
