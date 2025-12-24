using System.ComponentModel;
using McpServer.Template.Contracts.Constants;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Resources;

[McpServerResourceType]
public sealed class WelcomeResource
{
    [McpServerResource(
        UriTemplate = ResourceUris.Welcome,
        Name = "Welcome Message",
        MimeType = "text/plain")]
    [Description("Static welcome message for new users")]
    public Task<string> GetContentAsync(CancellationToken cancellationToken = default)
    {
        const string content = """
            Welcome to McpServer.Template!
            
            This is a demonstration of a clean, scalable Model Context Protocol (MCP) server 
            built with .NET 8 and proper architecture.
            
            Features:
            - Shared business logic between stdio and HTTP transports
            - Strict separation of concerns (Contracts / Application / Infrastructure / MCP Adapter / Hosts)
            - Easy addition of tools, prompts, and resources
            - Testable, idiomatic .NET code with DI and Options pattern
            
            Available Tools:
            - echo: Echoes back your message with a timestamp
            - calc_divide: Divides two numbers
            
            Available Prompts:
            - greeting: Generates a multilingual greeting message
            
            Available Resources:
            - resource://welcome: This welcome message
            - resource://status: Dynamic server status information
            """;

        return Task.FromResult(content);
    }
}
