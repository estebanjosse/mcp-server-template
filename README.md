# MCP Server Template - .NET 8

A production-ready Model Context Protocol (MCP) server in .NET 8 with clean architecture, demonstrating proper SDK integration patterns.

Uses the official **ModelContextProtocol SDK v0.5.0-preview.1** with a custom abstraction layer for maximum flexibility and testability.

## Architecture

This project follows strict clean architecture principles with clear separation of concerns:

```
McpServer.Abstractions/              # Stable Public API (No Dependencies)
├── ITool.cs                        # Tool abstraction
├── SimpleToolBase.cs               # Easy tool base class (auto-generates schema)
├── IPrompt.cs                      # Prompt abstraction
├── IResource.cs                    # Resource abstraction
└── IMcpServer.cs                   # Server abstraction

McpServer.Implementation.ModelContextProtocol/  # MCP SDK Adapter (Hidden)
├── ModelContextProtocolServerAdapter.cs        # Wraps ModelContextProtocol SDK v0.5.0-preview.1
├── McpServerOptions.cs                         # Configuration
└── ServiceCollectionExtensions.cs              # DI registration

McpServer.Examples/                  # ⭐ Shared Business Logic for BOTH transports
├── Tools/                          # Tool examples (work in STDIO & HTTP)
│   ├── EchoTool.cs
│   └── CalculatorTool.cs
├── Prompts/                        # Prompt examples (work in STDIO & HTTP)
│   ├── GreetingPrompt.cs
│   └── CodeReviewPrompt.cs
└── Resources/                      # Resource examples (work in STDIO & HTTP)
    ├── WelcomeResource.cs
    └── ServerStatusResource.cs

McpServer.Host/                     # 🖥️ Console Application (STDIO Transport)
└── Program.cs                      # For Claude Desktop, CLI tools

McpServer.Host.WebApi/              # 🌐 Web Application (HTTP/SSE Transport)
└── Program.cs                      # For web services, APIs
```

### Why Two Applications?

The console app ([McpServer.Host](McpServer.Host/Program.cs)) and web app ([McpServer.Host.WebApi](McpServer.Host.WebApi/Program.cs)) use different transports:
- **STDIO**: Reads from stdin, writes to stdout (for Claude Desktop)
- **HTTP/SSE**: REST API with Server-Sent Events (for web services)

Both applications share the **same business logic** from `McpServer.Examples` - when you add a new tool, it works in both transports automatically!

### Key Architecture Principles

1. **Dependency Inversion**: SDK (ModelContextProtocol v0.5.0-preview.1) is wrapped and not exposed in public API
2. **Stable Abstractions**: `McpServer.Abstractions` has zero external dependencies
3. **Adapter Pattern**: Implementation layer translates between abstractions and SDK
4. **SDK Replaceability**: Console app can use any MCP SDK without changing application code
5. **Shared Code**: Same tools/prompts/resources work in both STDIO and HTTP/SSE transports
6. **Simple Tool Creation**: `SimpleToolBase` auto-generates JSON schemas from method parameters

## Features

- **Clean Architecture**: SDK completely isolated behind stable interfaces
- **Dual Transport Support**:
  - **Console (STDIO)**: For Claude Desktop and CLI tools → `McpServer.Host`
  - **Web (HTTP/SSE)**: For web applications and APIs → `McpServer.Host.WebApi`
- **Shared Business Logic**: Add a tool once in `McpServer.Examples`, use in both transports
- **Dependency Injection**: Full Microsoft.Extensions.DependencyInjection support
- **Options Pattern**: Configuration with DataAnnotations validation
- **Easy Extensibility**: Just implement interfaces and register in DI

## Getting Started

### Prerequisites

- .NET 8 SDK or later

### Building

```bash
dotnet build
```

### Running

#### Console App (STDIO Transport) - For Claude Desktop

```bash
dotnet run --project McpServer.Host
```

#### Web App (HTTP/SSE Transport) - For Web Services

```bash
dotnet run --project McpServer.Host.WebApi
# Server starts at http://localhost:5000
# Test with: curl http://localhost:5000/health
```

### Testing the Web API

Once the web server is running:

```bash
# Check server health
curl http://localhost:5000/health

# Get server information and available capabilities
curl http://localhost:5000/

# List all tools, prompts, and resources
curl http://localhost:5000/capabilities

# Test with MCP Inspector
npx @modelcontextprotocol/inspector http://localhost:5000/mcp
```

### Configuration

Configuration can be provided via:

1. **Environment Variables**:
   - `MCP_TRANSPORT`: Transport type (`stdio` or `http`)
   - `MCP_HTTP_PORT`: HTTP port (when using HTTP transport)
   - `MCP_SERVER_NAME`: Server name
   - `MCP_SERVER_VERSION`: Server version

2. **appsettings.json**:
```json
{
  "McpServer": {
    "Transport": "stdio",
    "HttpPort": 5000,
    "ServerName": "MCP Server",
    "ServerVersion": "1.0.0"
  }
}
```

## Adding New Functionality

### Adding a New Tool (Simple Way)

For most tools, use `SimpleToolBase` which auto-generates the JSON schema:

```csharp
using System.ComponentModel;
using McpServer.Abstractions;

public class MyTool : SimpleToolBase
{
    public override string Name => "my-tool";
    public override string Description => "My custom tool";
    
    protected string Execute(
        [Description("Input parameter")] string input,
        [Description("Optional flag")] bool verbose = false)
    {
        return $"Result: {input}";
    }
}
```

**Benefits of SimpleToolBase**:
- Automatic JSON schema generation from method parameters
- Type conversion handled automatically
- Support for optional parameters (with defaults)
- [Description] attributes for parameter documentation
- Less boilerplate code

### Adding a New Tool (Advanced Way)

For complex tools with custom validation, implement `ITool` directly:

```csharp
public class MyComplexTool : ITool
{
    public string Name => "my-complex-tool";
    public string Description => "Tool with custom logic";
    
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            input = new { type = "string", description = "Input parameter" }
        },
        required = new[] { "input" }
    };

    public async Task<ToolResult> ExecuteAsync(
        IDictionary<string, object?>? arguments,
        CancellationToken cancellationToken = default)
    {
        // Your custom implementation with validation
        return new ToolResult("Result");
    }
}
```

### Register Your Tool

In `McpServer.Examples/ServiceCollectionExtensions.cs`:

```csharp
services.AddTransient<ITool, MyTool>();
```

### Adding a New Prompt

1. Create a class implementing `IPrompt`:

```csharp
public class MyPrompt : IPrompt
{
    public string Name => "my-prompt";
    public string Description => "My custom prompt";
    
    public IReadOnlyList<PromptArgument> Arguments => new List<PromptArgument>
    {
        new PromptArgument("name", "Parameter description", required: true)
    };

    public Task<IReadOnlyList<PromptMessage>> GetMessagesAsync(
        IDictionary<string, string>? arguments,
        CancellationToken cancellationToken = default)
    {
        // Your implementation
        var messages = new List<PromptMessage>
        {
            new PromptMessage("user", "Your prompt text")
        };
        return Task.FromResult<IReadOnlyList<PromptMessage>>(messages);
    }
}
```

2. Register it in DI:

```csharp
services.AddSingleton<IPrompt, MyPrompt>();
```

### Adding a New Resource

1. Create a class implementing `IResource`:

```csharp
public class MyResource : IResource
{
    public string Uri => "resource://my-resource";
    public string Name => "My Resource";
    public string? Description => "Description";
    public string MimeType => "text/plain";

    public Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        // Your implementation
        return Task.FromResult("Resource content");
    }
}
```

2. Register it in DI:

```csharp
services.AddSingleton<IResource, MyResource>();
```

## Examples

The server comes with several working examples:

### Tools
- **echo**: Echoes back a message
- **calculator**: Performs basic arithmetic operations (add, subtract, multiply, divide)

### Prompts
- **greeting**: Generates personalized greetings in multiple languages (en, fr, es, de)
- **code-review**: Creates structured code review prompt templates

### Resources
- **resource://welcome**: Static welcome message with server information
- **resource://server-status**: Dynamic server status information (JSON)

## Architecture Benefits

1. **SDK Isolation**: ModelContextProtocol SDK v0.5.0-preview.1 is completely hidden in `Implementation.ModelContextProtocol` layer
2. **Stable API**: `Abstractions` layer provides compile-time guarantees
3. **Testability**: All abstractions can be mocked for unit testing
4. **SDK Replaceability**: Swap SDK by creating new implementation layer
5. **Type Safety**: All contracts are strongly typed
6. **Validation**: Options validated on startup using DataAnnotations
7. **Clear Boundaries**: Each project has single responsibility
8. **Simple Tool Creation**: `SimpleToolBase` eliminates boilerplate for common tools

## License

MIT License

