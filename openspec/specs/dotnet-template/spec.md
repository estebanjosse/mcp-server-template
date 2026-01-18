# dotnet-template Specification

## Purpose
Defines the packaging and usage requirements for the dotnet new template that scaffolds MCP servers.

## Requirements
### Requirement: Template Baseline Scaffold

The dotnet template MUST register with the short name `mcp-server` and generate a runnable solution that includes at least one host project selected by the caller.

#### Scenario: Generate scaffold with HTTP host
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --name Contoso.Server --http-host`  
**Then** the generated solution MUST replace all occurrences of `McpServer.Template` with `Contoso.Server`  
**And** the HTTP host project MUST be included and build successfully with `dotnet build`

#### Scenario: Generate scaffold with both hosts
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host --stdio-host`  
**Then** both host projects MUST be created in the scaffold  
**And** each host MUST reference the shared projects so that both entry points compile

#### Scenario: Fail when no host selected
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server` without `--http-host` or `--stdio-host`  
**Then** the command MUST exit with a validation error explaining that at least one host option must be supplied  
**And** no partial solution SHALL be created on disk

---

### Requirement: Optional Content Flags

The template MUST expose boolean parameters that opt into sample tools and test projects without including them by default.

#### Scenario: Include sample tools on request
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host --include-sample-tools`  
**Then** the generated `Mcp` project MUST contain the existing sample tools under `Tools/`

#### Scenario: Exclude sample tools by default
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host`  
**Then** the generated `Mcp` project MUST contain an empty `Tools/` directory with a placeholder to keep the folder committed

#### Scenario: Include tests on request
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host --include-tests`  
**Then** the solution MUST include the test projects mirrored from the repository  
**And** `dotnet test` MUST succeed against the generated solution

---

### Requirement: Template Packaging Process

The repository MUST produce a NuGet package that contains the template configuration and assets, versioned in sync with the solution.

#### Scenario: Generate template package
**Given** a version is specified in the packaging configuration  
**When** the packaging task runs  
**Then** a `.nupkg` MUST be produced containing the template assets and `template.json`  
**And** the package version MUST match the specified version

#### Scenario: Install template from package
**Given** the generated `.nupkg` is available  
**When** a developer runs `dotnet new install McpServer.Template.<version>.nupkg`  
**Then** the template MUST become discoverable in `dotnet new list`  
**And** `dotnet new mcp-server` MUST use the installed package to scaffold projects

---

### Requirement: Template Documentation

The repository MUST document template usage, packaging, and publishing steps under `docs/`.

#### Scenario: Provide packaging guide
**Given** the template is supported  
**When** a developer opens the dedicated documentation in `docs/`  
**Then** the guide MUST explain template parameters, packaging commands, installation steps, and CI publishing expectations  
**And** the README MUST link to the guide once the template is released
