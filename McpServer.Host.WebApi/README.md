# MCP Server HTTP/SSE Transport

This project demonstrates how to run an MCP server using **HTTP with Server-Sent Events (SSE)** transport via the official ModelContextProtocol SDK.

## Architecture

Unlike the console application (`McpServer.Host`), this ASP.NET Core Web API uses the SDK's built-in HTTP transport:

- **No custom adapter needed** - Uses SDK's `WithHttpTransport()`
- **Automatic endpoint mapping** - `MapMcp("/mcp")` handles all MCP protocol messages
- **Stateful sessions** - Maintains session state between requests
- **SSE streaming** - Real-time bidirectional communication

## Running the Server

### Development
```bash
dotnet run --project McpServer.Host.WebApi
```

The server will start on `http://localhost:5000`

### Production
```bash
dotnet run --project McpServer.Host.WebApi --configuration Release
```

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/mcp` | GET | SSE connection for MCP protocol |
| `/mcp` | POST | Send MCP protocol messages |
| `/health` | GET | Health check endpoint |
| `/` | GET | Server information |

## Configuration

Configure via `appsettings.json` or environment variables:

```json
{
  "McpServer": {
    "ServerName": "MCP Server (HTTP)",
    "ServerVersion": "1.0.0"
  },
  "Urls": "http://localhost:5000"
}
```

Environment variables:
```bash
export McpServer__ServerName="My MCP Server"
export McpServer__ServerVersion="2.0.0"
export ASPNETCORE_URLS="http://localhost:8080"
```

## Testing with MCP Inspector

```bash
npx @modelcontextprotocol/inspector http://localhost:5000/mcp
```

## Testing with curl

### Health Check
```bash
curl http://localhost:5000/health
```

### SSE Connection
```bash
curl -N -H "Accept: text/event-stream" http://localhost:5000/mcp
```

### Call a Tool
```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "echo",
      "arguments": {
        "message": "Hello from HTTP!"
      }
    }
  }'
```

## Differences from Console App

| Feature | Console App (`McpServer.Host`) | Web API (`McpServer.Host.WebApi`) |
|---------|-------------------------------|----------------------------------|
| Transport | STDIO (stdin/stdout) | HTTP/SSE |
| Architecture | Custom adapter pattern | Direct SDK integration |
| Deployment | Command-line tool | Web server |
| Session State | Single session | Multiple concurrent sessions |
| Best For | Claude Desktop, CLI tools | Web clients, distributed systems |

## Architecture Benefits

### SDK Integration
- **Zero custom protocol code** - SDK handles all MCP protocol details
- **Built-in HTTP/SSE** - Production-ready transport layer
- **Session management** - Automatic session lifecycle handling

### Shared Core
- **Same tools, prompts, resources** - Reuses `McpServer.Examples`
- **No duplication** - Business logic defined once
- **Consistent behavior** - Same functionality across transports

## When to Use HTTP vs STDIO

### Use HTTP/SSE when:
- ✅ Building web-based AI applications
- ✅ Need multiple concurrent clients
- ✅ Deploying to cloud platforms
- ✅ Integrating with web services
- ✅ Require authentication/authorization

### Use STDIO when:
- ✅ Integrating with Claude Desktop
- ✅ Building command-line tools
- ✅ Simple single-user scenarios
- ✅ Local development/testing
- ✅ Process-based isolation needed

## Deployment

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 5000
ENTRYPOINT ["dotnet", "McpServer.Host.WebApi.dll"]
```

### Azure App Service
```bash
az webapp up --name mcp-server --resource-group myResourceGroup
```

### AWS Elastic Beanstalk
```bash
eb init -p "64bit Amazon Linux 2023 v3.0.0 running .NET 8" mcp-server
eb create mcp-server-env
```
