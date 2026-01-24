# Development Guide

This document is for contributors working directly on the `McpServer.Template` repository or on projects generated from it.

## Building and testing the template repository

From the root of this repository:

```powershell
# Restore dependencies and build all projects
dotnet build

# Run all tests
dotnet test

# Get code coverage (if using coverlet)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```

## Running the sample hosts in this repo

The repository includes sample hosts that exercise the template implementation.

### Stdio host

```powershell
# Run the stdio host
dotnet run --project src/McpServer.Template.Host.Stdio
```

To use MCP Inspector:

```powershell
npm install -g @modelcontextprotocol/inspector
mcp-inspector dotnet run --project src/McpServer.Template.Host.Stdio
```

### HTTP host

```powershell
# Run the HTTP host
dotnet run --project src/McpServer.Template.Host.Http
```

Useful endpoints:

- MCP endpoint: `http://localhost:5000/mcp`
- Health endpoint: `http://localhost:5000/health`
- Metrics endpoint (when enabled): `http://localhost:5000/metrics`

## Metrics and health checks

Operational metrics are disabled by default. Enable them with configuration or environment variables:

- Configuration key: `Metrics:Enabled`
- Environment variable: `MCP_METRICS_ENABLED`

Example:

```powershell
$env:MCP_METRICS_ENABLED = "true"
dotnet run --project src/McpServer.Template.Host.Http
curl http://localhost:5000/metrics
```

The health endpoint is always available on `/health` and is suitable for liveness/readiness probes and monitoring.

## Docker and GHCR

For full operational and Docker guidance (including building images, running containers, and using pre-built images from GHCR), see [docs/operations.md](operations.md).

## Extending the template (tools, prompts, resources)

When adding new capabilities to a generated project:

1. Define shared DTOs and constants in the Contracts project.
2. Implement business logic in the Application project.
3. Expose that logic via tools, prompts, or resources in the MCP adapter project.

### Adding a new tool

Create an interface and implementation in the Application layer, register it with dependency injection, and surface it through a tool in the MCP project. For example:

```csharp
public interface IMyService
{
    Task<MyResponse> DoSomethingAsync(MyRequest request, CancellationToken ct);
}
```

```csharp
services.AddScoped<IMyService, MyService>();
```

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

### Adding a new prompt

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

### Adding a new resource

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

## Test layout

Generated projects typically include:

- `*.Application.Tests` – business logic validation (services).
- `*.Infrastructure.Tests` – technical provider tests (clock, app info, etc.).
- `*.Mcp.Tests` – MCP adapter tests (tools, prompts, resources with mocked services).

Run tests from the root of a generated project:

```powershell
dotnet test
```

Use `--logger "console;verbosity=detailed"` or project filters if you need more granular runs.

## Configuration and dependencies

Configuration for generated hosts is handled through `appsettings*.json` files and environment variables. See the generated `appsettings.json` files under the `Host.Stdio` and `Host.Http` projects for examples.

Core dependencies (MCP SDK, ASP.NET Core, testing libraries, etc.) are centralized in the `Directory.Build.props` file of the generated solution or this template repository.
