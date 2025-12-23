using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Interfaces.Tools;

/// <summary>
/// A simple echo tool that returns the input message.
/// </summary>
public class EchoTool : ITool
{
    public string Name => "echo";

    public string Description => "Echoes back the provided message";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            message = new
            {
                type = "string",
                description = "The message to echo back"
            }
        },
        required = new[] { "message" }
    };

    public Task<ToolResult> ExecuteAsync(IDictionary<string, object?>? arguments, CancellationToken cancellationToken = default)
    {
        if (arguments == null || !arguments.TryGetValue("message", out var messageObj) || messageObj == null)
        {
            return Task.FromResult(new ToolResult("Error: message parameter is required", isError: true));
        }

        var message = messageObj.ToString();
        return Task.FromResult(new ToolResult($"Echo: {message}"));
    }
}
