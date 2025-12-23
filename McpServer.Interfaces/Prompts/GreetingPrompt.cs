using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Interfaces.Prompts;

/// <summary>
/// A simple greeting prompt that generates a personalized greeting message.
/// </summary>
public class GreetingPrompt : IPrompt
{
    public string Name => "greeting";

    public string Description => "Generates a personalized greeting message";

    public IReadOnlyList<PromptArgument> Arguments => new List<PromptArgument>
    {
        new PromptArgument("name", "The name of the person to greet", required: true),
        new PromptArgument("language", "The language for the greeting (e.g., 'en', 'fr', 'es')", required: false)
    };

    public Task<IReadOnlyList<PromptMessage>> GetMessagesAsync(IDictionary<string, string>? arguments, CancellationToken cancellationToken = default)
    {
        var name = (arguments != null && arguments.TryGetValue("name", out var n)) ? n : "Friend";
        var language = (arguments != null && arguments.TryGetValue("language", out var l)) ? l.ToLowerInvariant() : "en";

        var greeting = language switch
        {
            "fr" => $"Bonjour {name}! Comment allez-vous aujourd'hui?",
            "es" => $"¡Hola {name}! ¿Cómo estás hoy?",
            "de" => $"Hallo {name}! Wie geht es Ihnen heute?",
            _ => $"Hello {name}! How are you today?"
        };

        var messages = new List<PromptMessage>
        {
            new PromptMessage("user", greeting)
        };

        return Task.FromResult<IReadOnlyList<PromptMessage>>(messages);
    }
}
