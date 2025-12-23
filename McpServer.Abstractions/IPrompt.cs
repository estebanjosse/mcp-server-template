using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Abstractions;

/// <summary>
/// Represents a prompt template that can be used by the MCP client.
/// </summary>
public interface IPrompt
{
    /// <summary>
    /// Gets the unique name of the prompt.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the prompt.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the list of arguments that the prompt accepts.
    /// </summary>
    IReadOnlyList<PromptArgument> Arguments { get; }

    /// <summary>
    /// Gets the prompt messages with the provided arguments.
    /// </summary>
    /// <param name="arguments">The arguments to use in the prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of prompt messages.</returns>
    Task<IReadOnlyList<PromptMessage>> GetMessagesAsync(IDictionary<string, string>? arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an argument that a prompt accepts.
/// </summary>
public class PromptArgument
{
    public PromptArgument(string name, string description, bool required = false)
    {
        Name = name;
        Description = description;
        Required = required;
    }

    /// <summary>
    /// Gets the name of the argument.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of the argument.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets a value indicating whether the argument is required.
    /// </summary>
    public bool Required { get; }
}

/// <summary>
/// Represents a message in a prompt.
/// </summary>
public class PromptMessage
{
    public PromptMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }

    /// <summary>
    /// Gets the role of the message (e.g., "user", "assistant", "system").
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets the content of the message.
    /// </summary>
    public string Content { get; }
}
