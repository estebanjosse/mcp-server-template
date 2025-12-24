using System.ComponentModel;
using McpServer.Template.Application.Services;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Prompts;

[McpServerPromptType]
public sealed class GreetingPrompt(IGreetingService greetingService)
{
    [McpServerPrompt(Name = "greeting")]
    [Description("Generates a greeting message in the specified language")]
    public async Task<string> GetPromptAsync(
        [Description("Language code (en, fr, es, de)")] string language = "en",
        CancellationToken cancellationToken = default)
    {
        var greeting = await greetingService.GetGreetingAsync(language, cancellationToken);
        return $"{greeting}\n\nYou can use the available tools to interact with the server.";
    }
}
