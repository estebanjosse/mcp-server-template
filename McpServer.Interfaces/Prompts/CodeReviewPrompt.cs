using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Interfaces.Prompts;

/// <summary>
/// A code review prompt that generates a code review template.
/// </summary>
public class CodeReviewPrompt : IPrompt
{
    public string Name => "code-review";

    public string Description => "Generates a structured code review prompt";

    public IReadOnlyList<PromptArgument> Arguments => new List<PromptArgument>
    {
        new PromptArgument("language", "The programming language of the code", required: true),
        new PromptArgument("focus", "Areas to focus on (e.g., 'security', 'performance', 'readability')", required: false)
    };

    public Task<IReadOnlyList<PromptMessage>> GetMessagesAsync(IDictionary<string, string>? arguments, CancellationToken cancellationToken = default)
    {
        var language = (arguments != null && arguments.TryGetValue("language", out var l)) ? l : "code";
        var focus = (arguments != null && arguments.TryGetValue("focus", out var f)) ? f : "general quality";

        var systemPrompt = $@"You are an expert {language} code reviewer. Your task is to provide constructive feedback focusing on {focus}.

Consider the following aspects:
- Code correctness and logic
- Best practices and conventions
- Performance implications
- Security considerations
- Maintainability and readability
- Test coverage

Provide specific, actionable feedback with examples where appropriate.";

        var userPrompt = $"Please review the following {language} code with a focus on {focus}:";

        var messages = new List<PromptMessage>
        {
            new PromptMessage("system", systemPrompt),
            new PromptMessage("user", userPrompt)
        };

        return Task.FromResult<IReadOnlyList<PromptMessage>>(messages);
    }
}
