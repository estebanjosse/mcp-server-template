# Project Context

## Purpose
A clean, scalable, production-ready template for building Model Context Protocol (MCP) servers using the official C# SDK (.NET 8). This template demonstrates best practices for:
- Strict separation of concerns with MCP SDK isolation
- Shared business logic that works across multiple transport mechanisms (stdio and HTTP/SSE)
- Full testability with unit tests for all layers
- Idiomatic .NET patterns (DI, Options pattern, async/await)

**Goal**: Provide a starting point for building robust, maintainable MCP servers that follow hexagonal architecture principles and can easily be extended with new tools, prompts, and resources.

## Tech Stack
- **.NET 8.0** - Target framework with C# latest language version
- **Model Context Protocol SDK 0.5.0-preview.1** - Official MCP C# SDK
- **ASP.NET Core** - HTTP/SSE transport host
- **Microsoft.Extensions** (v10.0.0) - Dependency injection, configuration, hosting, logging
- **xUnit 2.9.2** - Unit testing framework
- **FluentAssertions 6.12.1** - Assertion library for readable test assertions
- **NSubstitute 5.3.0** - Mocking framework for test doubles
- **Coverlet 6.0.2** - Code coverage tool

## Project Conventions

### Code Style
- **Nullable Reference Types**: Enabled across all projects
- **Implicit Usings**: Enabled for cleaner code
- **File-Scoped Namespaces**: Preferred to reduce indentation
- **Sealed Classes**: Use `sealed` for concrete service implementations
- **Naming Conventions**:
  - Interfaces: Prefix with `I` (e.g., `ICalculatorService`, `ISystemClock`)
  - DTOs: Suffix with meaningful type (e.g., `CalcRequest`, `CalcResponse`)
  - Private fields: Use `_camelCase` with underscore prefix
  - Test methods: Use `MethodName_Scenario_ExpectedResult` pattern
  - Constants: Use `PascalCase` in dedicated `Constants` folders
- **Async/Await**: All I/O operations are async with `CancellationToken` support
- **Records**: Use for immutable DTOs and request/response objects

### Architecture Patterns
**Layered Hexagonal Architecture** with strict dependency flow:

```
Presentation → MCP Adapter → Application → Infrastructure/Contracts
```

**Layer Responsibilities**:
1. **Contracts** (`McpServer.Template.Contracts`): DTOs, constants, no dependencies
2. **Infrastructure** (`McpServer.Template.Infrastructure`): Technical services (ISystemClock, IAppInfoProvider)
3. **Application** (`McpServer.Template.Application`): Business logic ports and services (MCP-agnostic)
4. **MCP Adapter** (`McpServer.Template.Mcp`): MCP SDK integration (Tools, Prompts, Resources)
5. **Hosts** (`Host.Stdio`, `Host.Http`): Transport-specific entry points

**Key Principles**:
- Only the MCP project references MCP SDK packages
- Application layer is transport-agnostic and fully testable
- Ports and Adapters pattern: Application defines interfaces (ports), MCP layer implements adapters
- Dependency Injection: Register services via extension methods (`AddMcpTemplateModules()`)
- Each tool/prompt/resource delegates to application services for business logic

### Testing Strategy
- **Framework**: xUnit with FluentAssertions and NSubstitute
- **Coverage**: Unit tests for all services, tools, prompts, and resources
- **Test Structure**: Arrange-Act-Assert (AAA) pattern
- **Test Naming**: `MethodName_Scenario_ExpectedResult` (e.g., `DivideAsync_WhenDivisorIsZero_ShouldThrowInvalidOperationException`)
- **Assertions**: Use FluentAssertions for readable assertions (`.Should().Be()`, `.Should().ThrowAsync<>()`)
- **Mocking**: Use NSubstitute for mocking dependencies (e.g., `Substitute.For<ISystemClock>()`)
- **Theory Tests**: Use `[Theory]` with `[InlineData]` for parameterized tests
- **Test Organization**: Mirror source structure (tests match namespace/folder of source)
- **System Under Test**: Name as `_sut` in test classes

### Git Workflow
- **Commits**: Conventional Commits format (e.g., `feat:`, `fix:`, `docs:`, `test:`, `refactor:`)
- **OpenSpec**: Use OpenSpec for change proposals and specifications (see `openspec/AGENTS.md`)
- **Change Proposals**: Document significant changes in `openspec/changes/` before implementation

## Domain Context
**Model Context Protocol (MCP)**: A protocol for integrating context into Large Language Model (LLM) applications. MCP servers expose:
- **Tools**: Functions that LLMs can invoke (e.g., calculator, echo)
- **Prompts**: Reusable prompt templates with arguments (e.g., greeting with language parameter)
- **Resources**: Static or dynamic data sources (e.g., welcome message, server status)

**Transports**: MCP supports multiple transport mechanisms:
- **stdio**: Standard input/output (for Claude Desktop, CLI tools)
- **HTTP/SSE**: HTTP with Server-Sent Events (for web applications)

This template demonstrates how to build MCP servers that work with both transports while sharing the same business logic.

## Important Constraints
- **MCP SDK Isolation**: Only `McpServer.Template.Mcp` project may reference MCP SDK packages
- **No Warnings as Errors**: `TreatWarningsAsErrors` is set to `false` in Directory.Build.props
- **Documentation**: XML documentation generation is disabled (`GenerateDocumentationFile=false`)
- **.NET 8 Minimum**: Project requires .NET 8 SDK or later
- **Centralized Versioning**: All package versions managed in `Directory.Build.props`
- **Namespace Alignment**: Namespaces must match folder structure
- **Async Throughout**: All public service methods must be async with CancellationToken support

## External Dependencies
- **Model Context Protocol SDK**: Official C# SDK from Anthropic/ModelContextProtocol organization
  - Version managed via `$(ModelContextProtocolVersion)` in Directory.Build.props
  - Packages: `ModelContextProtocol.SDK`, `ModelContextProtocol.AspNetCore`
- **Microsoft.Extensions**: Dependency injection, configuration, hosting abstractions
  - Core DI, Logging, Options pattern
- **ASP.NET Core**: Used only in HTTP host for web-based transport
- **No External APIs**: Template is self-contained with no external service dependencies
- **No Database**: All data is in-memory or computed on demand
