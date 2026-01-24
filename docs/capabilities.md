# Capabilities

This template includes example MCP tools, prompts, and resources to demonstrate common patterns and serve as a starting point for your own implementation.

## Tools

The MCP adapter project (`McpServer.Template.Mcp`) exposes the following sample tools:

- **`echo`**
  - Echoes back a message along with a UTC timestamp.
  - Useful as a connectivity and round-trip test tool.
- **`calc_divide`**
  - Divides two numbers and returns the result.
  - Includes validation for division-by-zero scenarios.

You can add more tools by creating additional classes in `src/McpServer.Template.Mcp/Tools` and wiring them to services in `McpServer.Template.Application`.

## Prompts

The template also includes sample prompts:

- **`greeting`**
  - Generates a multilingual greeting message.
  - Currently supports `en`, `fr`, `es`, and `de`.

Prompts live in `src/McpServer.Template.Mcp/Prompts` and can compose responses from application services and infrastructure providers.

## Resources

Two resources are provided as examples of static and dynamic content:

- **`resource://welcome`**
  - Static welcome message that introduces the server and its features.
- **`resource://status`**
  - Dynamic status resource that reports uptime, version, and current timestamp.

Resources are implemented in `src/McpServer.Template.Mcp/Resources`. They often depend on infrastructure abstractions (for example, time and app info) to remain testable.

## Extending Capabilities

When adding custom behavior:

1. Define DTOs and constants in `McpServer.Template.Contracts` if they are shared across layers.
2. Implement or extend services in `McpServer.Template.Application` to hold business logic.
3. Expose those services via new tools, prompts, or resources in `McpServer.Template.Mcp`.

This flow keeps the MCP adapter thin and focused on protocol concerns while centralizing business rules in the application layer.
