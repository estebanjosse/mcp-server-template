using McpServer.Template.Application.Services;

namespace McpServer.Template.Application.Tests.Services;

public sealed class GreetingServiceTests
{
    private readonly GreetingService _sut = new();

    [Theory]
    [InlineData("en", "Hello! Welcome to the MCP Server Template.")]
    [InlineData("fr", "Bonjour ! Bienvenue sur le modèle de serveur MCP.")]
    [InlineData("es", "¡Hola! Bienvenido a la plantilla de servidor MCP.")]
    [InlineData("de", "Hallo! Willkommen bei der MCP-Server-Vorlage.")]
    public async Task GetGreetingAsync_ShouldReturnCorrectGreeting(string language, string expected)
    {
        // Act
        var result = await _sut.GetGreetingAsync(language);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("Fr")]
    [InlineData("ES")]
    public async Task GetGreetingAsync_ShouldBeCaseInsensitive(string language)
    {
        // Act
        var result = await _sut.GetGreetingAsync(language);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("xyz")]
    public async Task GetGreetingAsync_WithUnknownLanguage_ShouldReturnEnglishGreeting(string language)
    {
        // Act
        var result = await _sut.GetGreetingAsync(language);

        // Assert
        result.Should().Be("Hello! Welcome to the MCP Server Template.");
    }
}
