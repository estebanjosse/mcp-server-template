# EasyMcp.McpServer.Template

[![CI](https://github.com/estebanjosse/mcp-server-template/actions/workflows/ci.yml/badge.svg)](https://github.com/estebanjosse/mcp-server-template/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/10.0)

A `dotnet new` template that scaffolds a clean, scalable, production-ready Model Context Protocol (MCP) server using [the official C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

This [NuGet package](https://www.nuget.org/packages/EasyMcp.McpServer.Template) is a **template**, not a runtime library: installing it adds a `mcp-server` template to your `dotnet new` list so you can generate full MCP server solutions.

## 🚀 Quick start

### 1. Prerequisites

- .NET 8 SDK or later
- (Optional) an MCP client such as MCP Inspector for testing the stdio host

### 2. Install the template

```bash
dotnet new install EasyMcp.McpServer.Template
```

### 3. Create a new MCP server

Create a server that includes both HTTP and stdio hosts plus tests:

```bash
dotnet new mcp-server \
  --name MyCompany.McpServer \
  --http-host \
  --stdio-host \
  --include-tests \
  -o my-server
```

Other combinations are supported (HTTP-only, stdio-only, without tests); see the [template documentation](docs/template.md) for all options.

### 4. Build and run the generated hosts

From the generated directory:

```bash
cd my-server

# HTTP host
dotnet run --project src/MyCompany.McpServer.Host.Http

# Stdio host
dotnet run --project src/MyCompany.McpServer.Host.Stdio
```

Configuration is managed via `appsettings*.json` files and environment variables (for example, enabling metrics with `MCP_METRICS_ENABLED=true`).

## 📚 Documentation

- Template usage and options: see [docs/template.md](docs/template.md).
- Architecture and project layout: see [docs/architecture.md](docs/architecture.md).
- Built-in tools, prompts, and resources: see [docs/capabilities.md](docs/capabilities.md).
- Running in development, health checks, metrics, Docker, GHCR: see [docs/operations.md](docs/operations.md).

## 🎯 Architecture overview

The generated solution follows a **layered architecture** that keeps MCP SDK usage isolated while sharing business logic across transports:

- **Presentation layer** – HTTP and stdio hosts.
- **MCP adapter layer** – tools, prompts, and resources that speak the MCP protocol.
- **Application layer** – pure business services (no MCP SDK).
- **Infrastructure and contracts** – DTOs, constants, and technical providers (time, app info, etc.).

See [docs/architecture.md](docs/architecture.md) for the full diagram, design principles, and detailed project structure.

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

See [docs/capabilities.md](docs/capabilities.md) for a capabilities-focused overview of tools, prompts, and resources.

### Prompts
- **`greeting`**: Generates a multilingual greeting message (en, fr, es, de)

### Resources
- **`resource://welcome`**: Static welcome message with feature overview
- **`resource://status`**: Dynamic server status (uptime, version, timestamp)

## 🧑‍💻 Developing and operating

If you are working on this repository or on a project generated from the template, you can:

- Follow the development guide in [docs/development.md](docs/development.md) for
  - building and testing the solution,
  - running the sample HTTP and stdio hosts,
  - adding new tools, prompts, and resources, and
  - understanding the test layout and configuration.
- Follow [docs/operations.md](docs/operations.md) for operational topics such as metrics, health checks, Docker images, and GitHub Container Registry (GHCR).

## 📚 Learn more

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [MCP .NET SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)

## 📄 License

This project is licensed under the MIT License – see [LICENSE](LICENSE) for details.

Third-party dependency attribution guidance is documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## 🙏 Acknowledgments

Built with the official [ModelContextProtocol C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).
