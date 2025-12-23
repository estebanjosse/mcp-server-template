using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Interfaces.Resources;

/// <summary>
/// A simple text resource that provides static information.
/// </summary>
public class WelcomeResource : IResource
{
    public string Uri => "resource://welcome";

    public string Name => "Welcome Message";

    public string? Description => "A welcome message for new users";

    public string MimeType => "text/plain";

    public Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        var content = @"Welcome to the MCP Server!

This server demonstrates the Model Context Protocol (MCP) implementation in .NET 8.

Available features:
- Tools: Execute various operations (echo, calculator)
- Prompts: Generate templated messages (greeting, code-review)
- Resources: Access static or dynamic content

The server supports both stdio and HTTP (SSE) transports.

For more information, visit: https://modelcontextprotocol.io";

        return Task.FromResult(content);
    }
}
