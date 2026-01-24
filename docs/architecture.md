# Architecture

This template implements a layered, testable architecture for MCP servers built with .NET 8. It keeps MCP SDK usage isolated while sharing business logic across multiple transports.

## Layered Design

From top to bottom, the solution is organized into four main layers:

1. **Presentation layer (hosts)**
   - `McpServer.Template.Host.Stdio`: console host that exposes the MCP server over stdio.
   - `McpServer.Template.Host.Http`: ASP.NET Core host that exposes the MCP server over HTTP/SSE.
2. **MCP adapter layer**
   - `McpServer.Template.Mcp`: the only project that references MCP SDK packages.
   - Implements tools, prompts, and resources, and wires them to application services.
3. **Business logic layer**
   - `McpServer.Template.Application`: contains pure application services and ports.
   - No direct dependency on MCP SDK or ASP.NET Core.
4. **Infrastructure & contracts layer**
   - `McpServer.Template.Contracts`: DTOs and constants shared across layers.
   - `McpServer.Template.Infrastructure`: technical providers such as time, app info, etc.

This layering allows hosts and the MCP adapter to change independently of core business logic.

## Architecture Diagram

```text
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

## Key Design Principles

- **SDK isolation**: Only `McpServer.Template.Mcp` references MCP SDK packages; other projects stay MCP-agnostic.
- **Shared business logic**: Both stdio and HTTP hosts call into the same application services.
- **High testability**: Application and infrastructure layers are covered by unit tests without needing MCP or transport concerns.
- **Scalability**: New tools, prompts, and resources can be added without changing the core architecture.

## Project Structure

At a high level, the solution is organized as:

```text
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
```

Use this as the primary reference when extending the template with new transports, services, or integrations.
