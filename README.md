# McpServer.Template

[![CI](https://github.com/estebanjosse/mcp-server-template/actions/workflows/ci.yml/badge.svg)](https://github.com/estebanjosse/mcp-server-template/actions/workflows/ci.yml)

A clean, scalable, production-ready implementation of a Model Context Protocol (MCP) server using the official C# SDK (.NET 8).

## 🎯 Architecture Overview

This template demonstrates a **strict separation of concerns** with a layered architecture that keeps MCP SDK dependencies isolated while maintaining a shared business logic foundation:

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  ┌─────────────────────┐      ┌─────────────────────┐      │
│  │  Host.Stdio         │      │  Host.Http          │      │
│  │  (Console)          │      │  (ASP.NET Core)     │      │
│  │  stdio transport    │      │  HTTP/SSE transport │      │
│  └──────────┬──────────┘      └──────────┬──────────┘      │
└─────────────┼─────────────────────────────┼─────────────────┘
              │                             │
              └──────────┬──────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                    MCP Adapter Layer                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  McpServer.Template.Mcp                              │  │
│  │  - Tools (EchoTool, CalcDivideTool)                  │  │
│  │  - Prompts (GreetingPrompt)                          │  │
│  │  - Resources (WelcomeResource, StatusResource)       │  │
│  │  - MCP SDK Integration Only                          │  │
│  └─────────────────────┬────────────────────────────────┘  │
└────────────────────────┼────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│                   Business Logic Layer                       │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  McpServer.Template.Application                      │  │
│  │  - IEchoService / ICalculatorService                 │  │
│  │  - IStatusService / IGreetingService                 │  │
│  │  - Pure business logic (no MCP SDK)                  │  │
│  └─────────────────────┬────────────────────────────────┘  │
└────────────────────────┼────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│              Infrastructure & Contracts Layer                │
│  ┌──────────────────────┐  ┌───────────────────────────┐  │
│  │  Infrastructure      │  │  Contracts                │  │
│  │  - ISystemClock      │  │  - DTOs                   │  │
│  │  - IAppInfoProvider  │  │  - Constants              │  │
│  └──────────────────────┘  └───────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Key Design Principles

✅ **SDK Isolation**: Only `McpServer.Template.Mcp` project references MCP SDK packages  
✅ **Shared Business Logic**: Same services used by both stdio and HTTP transports  
✅ **Testability**: All layers fully unit testable with xUnit, FluentAssertions, and NSubstitute  
✅ **Scalability**: Easy to add new tools, prompts, and resources  
✅ **Idiomatic .NET**: Dependency Injection, Options pattern, async/await throughout

## 📁 Project Structure

```
McpServer.Template/
├── src/
│   ├── McpServer.Template.Contracts          # DTOs and constants (no dependencies)
│   ├── McpServer.Template.Application        # Business services (testable, MCP-agnostic)
│   ├── McpServer.Template.Infrastructure     # Technical providers (ISystemClock, IAppInfoProvider)
│   ├── McpServer.Template.Mcp                # MCP adapter (Tools, Prompts, Resources)
│   ├── McpServer.Template.Host.Stdio         # Console host with stdio transport
│   └── McpServer.Template.Host.Http          # ASP.NET Core host with HTTP/SSE transport
├── tests/
│   ├── McpServer.Template.Application.Tests  # Service unit tests
│   ├── McpServer.Template.Infrastructure.Tests # Infrastructure unit tests
│   └── McpServer.Template.Mcp.Tests          # MCP adapter unit tests
├── Directory.Build.props                      # Centralized package versions and properties
├── Directory.Build.targets                    # Custom MSBuild targets
└── McpServer.Template.sln                     # Solution file
```

## 🚀 Features

### Tools
- **`echo`**: Echoes back a message with a UTC timestamp
- **`calc_divide`**: Divides two numbers (with division-by-zero validation)

### Prompts
- **`greeting`**: Generates a multilingual greeting message (en, fr, es, de)

### Resources
- **`resource://welcome`**: Static welcome message with feature overview
- **`resource://status`**: Dynamic server status (uptime, version, timestamp)

## 🛠️ Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- (Optional) [MCP Inspector](https://github.com/modelcontextprotocol/inspector) for stdio testing

### Build the Solution

```powershell
# Restore dependencies and build all projects
dotnet build

# Run tests
dotnet test

# Get test coverage (if using coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

## 🎮 Running the Server

### Option 1: Stdio Transport (Console)

The stdio transport is ideal for MCP Inspector or command-line integration:

```powershell
# Run the stdio host
dotnet run --project src/McpServer.Template.Host.Stdio

# Or with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run --project src/McpServer.Template.Host.Stdio
```

**Testing with MCP Inspector:**
```powershell
# Install MCP Inspector globally (Node.js required)
npm install -g @modelcontextprotocol/inspector

# Launch inspector with stdio server
mcp-inspector dotnet run --project src/McpServer.Template.Host.Stdio
```

### Option 2: HTTP Transport (ASP.NET Core)

The HTTP transport exposes MCP over HTTP with Server-Sent Events (SSE):

```powershell
# Run the HTTP host
dotnet run --project src/McpServer.Template.Host.Http

# Server starts at http://localhost:5000
# MCP endpoint: http://localhost:5000/mcp
# Health endpoint: http://localhost:5000/health
```

**Testing with HTTP:**

```powershell
# Get server capabilities
curl http://localhost:5000/mcp

# Check health status
curl http://localhost:5000/health

# Call the echo tool (requires proper MCP client)
# Use an MCP-compatible HTTP client or build one using ModelContextProtocol.AspNetCore
```

**Health Endpoint:**

The HTTP host includes a `/health` endpoint using ASP.NET Core health check middleware:

```powershell
# Get health status (returns HTTP 200 OK with JSON)
curl http://localhost:5000/health

# Example response:
# {"status":"Healthy","checks":[]}
```

This endpoint is designed for:
- Container orchestration (Kubernetes liveness/readiness probes)
- Load balancer health checks
- Monitoring and alerting systems
- DevOps automation

The health endpoint requires no authentication and returns minimal information suitable for frequent polling.

### Prometheus Metrics

Operational metrics are disabled by default. Enable them by setting `Metrics:Enabled` to `true` in configuration (for example, [src/McpServer.Template.Host.Http/appsettings.json](src/McpServer.Template.Host.Http/appsettings.json)) or by exporting the `MCP_METRICS_ENABLED=true` environment variable. Once enabled, the host exposes a `/metrics` endpoint that serves Prometheus exposition format (`text/plain`) and includes:

- `mcp_requests_total`
- `mcp_tool_invocations_total`
- `mcp_tool_invocations_by_tool_total` (labelled by `tool`)
- `mcp_sessions_active`

Example workflow:

```powershell
$env:MCP_METRICS_ENABLED = "true"
dotnet run --project src/McpServer.Template.Host.Http
curl http://localhost:5000/metrics
```

Remember to remove or secure the metrics endpoint in production environments if it should not be publicly accessible.

## 🐳 Docker Deployment

The HTTP host can be containerized using the provided multi-stage Dockerfile with security hardening and build optimization.

### Building the Docker Image

```powershell
# Build the image
docker build -t mcp-server-template .

# Verify the image was created
docker images mcp-server-template
```

The Dockerfile uses:
- **Multi-stage build**: Separate build and runtime stages for minimal image size (~230MB)
- **Official Microsoft images**: `mcr.microsoft.com/dotnet/sdk:8.0` (build) and `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime)
- **Layer caching**: Project files copied before source code for faster rebuilds
- **Security hardening**: Non-root user (`mcpserver`), minimal base image
- **Health check**: Built-in Docker HEALTHCHECK using the `/health` endpoint

### Running the Container

```powershell
# Run the container with default settings (port 5000)
docker run -d -p 5000:5000 --name mcp-server mcp-server-template

# Test the endpoints
curl http://localhost:5000/health
curl http://localhost:5000/mcp

# View container logs
docker logs mcp-server

# Check container health status
docker ps --format "{{.Names}} - {{.Status}}"
```

### Configuration Options

**Custom Port:**
```powershell
# Run on port 8080
docker run -d -p 8080:8080 --name mcp-server mcp-server-template --urls http://+:8080
```

**Environment Variables:**
```powershell
# Set environment (Development/Production)
docker run -d -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  --name mcp-server \
  mcp-server-template

# Custom configuration file (mount volume)
docker run -d -p 5000:5000 \
  -v ${PWD}/appsettings.Production.json:/app/appsettings.Production.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --name mcp-server \
  mcp-server-template
```

**Docker Compose Example:**
```yaml
version: '3.8'
services:
  mcp-server:
    image: mcp-server-template
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 5s
    restart: unless-stopped
```

### Container Management

```powershell
# Stop the container
docker stop mcp-server

# Start the container
docker start mcp-server

# Remove the container
docker rm mcp-server

# View resource usage
docker stats mcp-server

# Execute commands in the container
docker exec mcp-server whoami  # Should output: mcpserver
```

### Troubleshooting

**Container won't start:**
```powershell
# Check logs for errors
docker logs mcp-server

# Run in foreground mode to see output
docker run --rm -p 5000:5000 mcp-server-template
```

**Port already in use:**
```powershell
# Use a different host port
docker run -d -p 5001:5000 --name mcp-server mcp-server-template
```

## 📦 Using Pre-built Images from GHCR

Pre-built Docker images are automatically published to GitHub Container Registry (GHCR) on every push to the main branch.

### Pulling Images from GHCR

```powershell
# Pull the latest image
docker pull ghcr.io/estebanjosse/mcp-server-template:latest

# Pull a specific version
docker pull ghcr.io/estebanjosse/mcp-server-template:v1.0.0

# Pull by commit SHA
docker pull ghcr.io/estebanjosse/mcp-server-template:sha-abc1234
```

### Running GHCR Images

```powershell
# Run the latest image from GHCR
docker run -d -p 5000:5000 --name mcp-server ghcr.io/estebanjosse/mcp-server-template:latest

# Test the server
curl http://localhost:5000/health
```

### Available Tags

- `latest` - Latest build from the main branch
- `main` - Latest build from the main branch (same as latest)
- `sha-<commit>` - Specific commit (e.g., `sha-abc1234`)
- `v<version>` - Semantic version tags (e.g., `v1.0.0`, `v1.0`, `v1`)

### Authentication (for private repositories)

If the repository is private, authenticate with GitHub:

```powershell
# Login to GHCR
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Pull the image
docker pull ghcr.io/estebanjosse/mcp-server-template:latest
```

**Health check failing:**
```powershell
# Inspect health check details
docker inspect mcp-server --format='{{json .State.Health}}' | ConvertFrom-Json

# Test health endpoint manually from inside container
docker exec mcp-server curl http://localhost:5000/health
```

### Security Scanning

```powershell
# Scan for vulnerabilities with Docker Scout
docker scout quickview mcp-server-template
docker scout cves mcp-server-template

# Scan with Trivy (if installed)
trivy image mcp-server-template
```

The image is built with security best practices:
- ✅ Runs as non-root user (`mcpserver`)
- ✅ Minimal base image (ASP.NET Core runtime only, no SDK)
- ✅ No unnecessary packages or tools
- ✅ Official Microsoft base images with security updates
- ✅ Regular security scanning recommended

## 📝 Adding New Features

### Adding a New Tool

1. Create interface and implementation in `Application/Services`:
   ```csharp
   public interface IMyService
   {
       Task<MyResponse> DoSomethingAsync(MyRequest request, CancellationToken ct);
   }
   ```

2. Register in `Application/Extensions/ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<IMyService, MyService>();
   ```

3. Create MCP tool in `Mcp/Tools`:
   ```csharp
   [McpServerToolType]
   public sealed class MyTool(IMyService myService)
   {
       [McpServerTool(Name = "my_tool")]
       [Description("Tool description")]
       public async Task<MyResponse> ExecuteAsync(
           [Description("Param description")] string param,
           CancellationToken ct = default)
       {
           return await myService.DoSomethingAsync(new MyRequest(param), ct);
       }
   }
   ```

### Adding a New Prompt

```csharp
[McpServerPromptType]
public sealed class MyPrompt(IMyService service)
{
    [McpServerPrompt(Name = "my_prompt")]
    [Description("Prompt description")]
    public async Task<string> GetPromptAsync(
        [Description("Argument description")] string argument = "default",
        CancellationToken ct = default)
    {
        return await service.GetPromptContentAsync(argument, ct);
    }
}
```

### Adding a New Resource

```csharp
[McpServerResourceType]
public sealed class MyResource(IDependency dep)
{
    [McpServerResource(
        Uri = "resource://my-resource",
        Name = "My Resource",
        MimeType = "text/plain")]
    [Description("Resource description")]
    public async Task<string> GetContentAsync(CancellationToken ct = default)
    {
        return await dep.GetResourceContentAsync(ct);
    }
}
```

## 🧪 Testing

The solution includes comprehensive unit tests:

```powershell
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests for a specific project
dotnet test tests/McpServer.Template.Application.Tests

# Generate coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

**Test Organization:**
- **Application.Tests**: Business logic validation (services)
- **Infrastructure.Tests**: Technical provider tests (clock, app info)
- **Mcp.Tests**: MCP adapter tests (tools, prompts, resources with mocked services)

## 🔧 Configuration

### Stdio Host (`src/McpServer.Template.Host.Stdio/appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ModelContextProtocol": "Debug"
    }
  },
  "McpServer": {
    "Name": "McpServer.Template",
    "Version": "1.0.0"
  }
}
```

### HTTP Host (`src/McpServer.Template.Host.Http/appsettings.json`)

```json
{
  "Urls": "http://localhost:5000",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ModelContextProtocol": "Debug"
    }
  }
}
```

Override via environment variables:
```powershell
$env:ASPNETCORE_URLS = "http://localhost:8080"
dotnet run --project src/McpServer.Template.Host.Http
```

## 📦 Dependencies

**Core Packages:**
- `ModelContextProtocol` (0.5.0-preview.1) - MCP SDK for stdio
- `ModelContextProtocol.AspNetCore` (0.5.0-preview.1) - MCP SDK for HTTP
- `Microsoft.Extensions.DependencyInjection` (10.0.0) - DI container
- `Microsoft.Extensions.Hosting` (10.0.0) - Generic host

**Test Packages:**
- `xunit` (2.9.2) - Test framework
- `FluentAssertions` (6.12.1) - Assertion library
- `NSubstitute` (5.3.0) - Mocking library
- `coverlet.collector` (6.0.2) - Code coverage

See [Directory.Build.props](Directory.Build.props) for complete package list.

## 📚 Learn More

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [MCP .NET SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Built with the official [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).
