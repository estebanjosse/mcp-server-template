## ADDED Requirements
### Requirement: Template Baseline Scaffold
The dotnet template MUST register with the short name `mcp-server` and generate a runnable solution that includes at least one host project selected by the caller.

#### Scenario: Generate scaffold with HTTP host
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server --name Contoso.Server --http-host`
- **THEN** the generated solution replaces all occurrences of `McpServer.Template` with `Contoso.Server`
- **AND** the HTTP host project is included and builds successfully with `dotnet build`

#### Scenario: Generate scaffold with both hosts
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server --http-host --stdio-host`
- **THEN** both host projects are created in the scaffold
- **AND** each host references the shared projects so that both entry points compile

#### Scenario: Fail when no host selected
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server` without `--http-host` or `--stdio-host`
- **THEN** the command exits with a validation error explaining that at least one host option must be supplied
- **AND** no partial solution is created on disk

### Requirement: Optional Content Flags
The template MUST expose boolean parameters that opt into sample tools and test projects without including them by default.

#### Scenario: Include sample tools on request
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server --http-host --include-sample-tools`
- **THEN** the generated `Mcp` project contains the existing sample tools under `Tools/`

#### Scenario: Exclude sample tools by default
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server --http-host`
- **THEN** the generated `Mcp` project contains an empty `Tools/` directory with a placeholder to keep the folder committed

#### Scenario: Include tests on request
- **GIVEN** the template package is installed
- **WHEN** a developer runs `dotnet new mcp-server --http-host --include-tests`
- **THEN** the solution includes the test projects mirrored from the repository
- **AND** `dotnet test` succeeds against the generated solution

### Requirement: Template Packaging Process
The repository MUST produce a NuGet package that contains the template configuration and assets, versioned in sync with the solution.

#### Scenario: Generate template package
- **GIVEN** the repository version is set in `Directory.Build.props`
- **WHEN** the packaging task runs
- **THEN** a `.nupkg` is produced containing the template assets and `template.json`
- **AND** the package version matches the value from `Directory.Build.props`

#### Scenario: Install template from package
- **GIVEN** the generated `.nupkg` is available
- **WHEN** a developer runs `dotnet new install McpServer.Template.<version>.nupkg`
- **THEN** the template becomes discoverable in `dotnet new list`
- **AND** `dotnet new mcp-server` uses the installed package to scaffold projects

### Requirement: Template Documentation
The repository MUST document template usage, packaging, and publishing steps under `docs/`.

#### Scenario: Provide packaging guide
- **GIVEN** the template is supported
- **WHEN** a developer opens the dedicated documentation in `docs/`
- **THEN** the guide explains template parameters, packaging commands, installation steps, and CI publishing expectations
- **AND** the README links to the guide once the template is released
