namespace McpServer.Template.Application.Services;

public sealed class GreetingService : IGreetingService
{
    private static readonly Dictionary<string, string> Greetings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "Hello! Welcome to the MCP Server Template.",
        ["fr"] = "Bonjour ! Bienvenue sur le modèle de serveur MCP.",
        ["es"] = "¡Hola! Bienvenido a la plantilla de servidor MCP.",
        ["de"] = "Hallo! Willkommen bei der MCP-Server-Vorlage."
    };

    public Task<string> GetGreetingAsync(string language, CancellationToken cancellationToken = default)
    {
        var greeting = Greetings.TryGetValue(language, out var value)
            ? value
            : Greetings["en"];

        return Task.FromResult(greeting);
    }
}
