using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Abstractions;

/// <summary>
/// Represents a tool that can be invoked by the MCP client.
/// </summary>
public interface ITool
{
    /// <summary>
    /// Gets the unique name of the tool.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the JSON schema for the tool's input parameters.
    /// </summary>
    object InputSchema { get; }

    /// <summary>
    /// Executes the tool with the provided arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the tool.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<ToolResult> ExecuteAsync(IDictionary<string, object?>? arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
public class ToolResult
{
    public ToolResult(string content, bool isError = false)
    {
        Content = content;
        IsError = isError;
    }

    /// <summary>
    /// Gets the content of the result.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents an error.
    /// </summary>
    public bool IsError { get; }
}
