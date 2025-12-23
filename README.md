# MCP Server Template - .NET 8

A minimal, production-ready implementation of a Model Context Protocol (MCP) server using the official C# SDK with clean architecture.

## Architecture

This project follows clean architecture principles with clear separation of concerns:

```
McpServer.Abstractions/    # Stable API - No SDK dependencies
├── ITool.cs              # Tool abstraction
├── IPrompt.cs            # Prompt abstraction
├── IResource.cs          # Resource abstraction
└── IMcpServer.cs         # Server abstraction

McpServer.Server/         # SDK Adapters
├── McpServerAdapter.cs   # SDK implementation
├── McpServerOptions.cs   # Configuration options
└── ServiceCollectionExtensions.cs

McpServer.Interfaces/     # Example Implementations
├── Tools/                # Tool examples
│   ├── EchoTool.cs
│   └── CalculatorTool.cs
├── Prompts/              # Prompt examples
│   ├── GreetingPrompt.cs
│   └── CodeReviewPrompt.cs
└── Resources/            # Resource examples
    ├── WelcomeResource.cs
    └── ServerStatusResource.cs

McpServer.Host/           # Entry Point
└── Program.cs            # Application host
```

## Features

- **Clean Architecture**: Stable abstractions layer, SDK adapter layer, and implementation layer
- **SDK Abstraction**: MCP SDK is hidden behind stable interfaces, making it easily replaceable
- **Dependency Injection**: Full DI support with Microsoft.Extensions.DependencyInjection
- **Configuration**: Options pattern with validation using DataAnnotations
- **Multiple Transports**: 
  - `stdio` - Standard input/output (default)
  - `http` - Server-Sent Events (SSE) over HTTP
- **Easy Extensibility**: Simple to add new tools, prompts, and resources

## Getting Started

### Prerequisites

- .NET 8 SDK or later

### Building

```bash
dotnet build
```

### Running

#### Stdio Transport (Default)

```bash
dotnet run --project McpServer.Host
```

Or set the transport explicitly:

```bash
MCP_TRANSPORT=stdio dotnet run --project McpServer.Host
```

#### HTTP Transport

```bash
MCP_TRANSPORT=http MCP_HTTP_PORT=5000 dotnet run --project McpServer.Host
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

### Adding a New Tool

1. Create a class implementing `ITool`:

```csharp
public class MyTool : ITool
{
    public string Name => "my-tool";
    public string Description => "My custom tool";
    
    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            input = new { type = "string", description = "Input parameter" }
        },
        required = new[] { "input" }
    };

    public Task<ToolResult> ExecuteAsync(
        IDictionary<string, object?>? arguments,
        CancellationToken cancellationToken = default)
    {
        // Your implementation
        return Task.FromResult(new ToolResult("Result"));
    }
}
```

2. Register it in DI:

```csharp
services.AddSingleton<ITool, MyTool>();
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
- **calculator**: Performs basic arithmetic operations

### Prompts
- **greeting**: Generates personalized greetings in multiple languages
- **code-review**: Creates code review prompt templates

### Resources
- **resource://welcome**: Static welcome message
- **resource://server-status**: Dynamic server status information

## License

MIT License

