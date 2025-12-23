# Quick Start Guide

Get started with the MCP Server Template in minutes!

## Running the Server

### Default (STDIO Transport)
```bash
dotnet run --project McpServer.Host
```

### HTTP Transport (requires ASP.NET Core)
```bash
MCP_TRANSPORT=http MCP_HTTP_PORT=5000 dotnet run --project McpServer.Host
```
*Note: HTTP/SSE transport is configured for stdio in console apps. For HTTP, see the ModelContextProtocol SDK ASP.NET Core integration.*

## Testing Tools

### Echo Tool
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "echo",
    "arguments": {
      "message": "Hello, MCP!"
    }
  }
}
```

### Calculator Tool
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "calculator",
    "arguments": {
      "operation": "add",
      "a": 5,
      "b": 3
    }
  }
}
```

## Testing Prompts

### Greeting Prompt
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "prompts/get",
  "params": {
    "name": "greeting",
    "arguments": {
      "name": "Alice",
      "language": "fr"
    }
  }
}
```

### Code Review Prompt
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "prompts/get",
  "params": {
    "name": "code-review",
    "arguments": {
      "language": "C#",
      "focus": "security"
    }
  }
}
```

## Testing Resources

### Welcome Resource
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "resources/read",
  "params": {
    "uri": "resource://welcome"
  }
}
```

### Server Status Resource
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "method": "resources/read",
  "params": {
    "uri": "resource://server-status"
  }
}
```

## Configuration

Environment variables:
- `MCP_TRANSPORT` - Transport type (`stdio` or `http`)
- `MCP_HTTP_PORT` - HTTP port when using HTTP transport (default: 5000)
- `MCP_SERVER_NAME` - Server name (default: "MCP Server")
- `MCP_SERVER_VERSION` - Server version (default: "1.0.0")

## Creating Your First Tool

The easiest way to add a tool is using `SimpleToolBase`:

```csharp
using System.ComponentModel;
using McpServer.Abstractions;

namespace McpServer.Examples.Tools;

public class MyFirstTool : SimpleToolBase
{
    public override string Name => "my_first_tool";
    public override string Description => "My first MCP tool";
    
    protected string Execute(
        [Description("Your name")] string name,
        [Description("Add greeting")] bool includeGreeting = true)
    {
        var result = includeGreeting 
            ? $"Hello, {name}!" 
            : name;
        return result;
    }
}
```

Then register it in `ServiceCollectionExtensions.cs`:
```csharp
services.AddTransient<ITool, MyFirstTool>();
```

**That's it!** The JSON schema is automatically generated from your method parameters.

## Architecture Benefits

1. **Clean Separation**: ModelContextProtocol SDK v0.6.0 is completely isolated
2. **Easy Testing**: Abstractions can be mocked for unit tests
3. **SDK Replacement**: Swap SDK without changing application code
4. **Simple Extension**: Use `SimpleToolBase` or implement `ITool` directly
5. **Zero Boilerplate**: Auto-generated JSON schemas from method parameters
5. **Type Safety**: All contracts are strongly typed
6. **Validation**: Options are validated on startup using DataAnnotations
