# Quick Start Guide

## Running the Server

### Default (STDIO)
```bash
dotnet run --project McpServer.Host
```

### HTTP Transport
```bash
MCP_TRANSPORT=http MCP_HTTP_PORT=5000 dotnet run --project McpServer.Host
```

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

## Architecture Benefits

1. **Clean Separation**: SDK is completely isolated in the Server layer
2. **Easy Testing**: Abstractions can be mocked for unit tests
3. **SDK Replacement**: The SDK can be swapped out without changing application code
4. **Simple Extension**: Just implement ITool, IPrompt, or IResource and register in DI
5. **Type Safety**: All contracts are strongly typed
6. **Validation**: Options are validated on startup using DataAnnotations
