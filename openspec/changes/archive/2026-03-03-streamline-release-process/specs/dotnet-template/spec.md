## MODIFIED Requirements

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

#### Scenario: Version placeholder for local development
**Given** the `McpServer.Template.csproj` is used for local development
**When** `dotnet pack` is run without a `-p:Version` override
**Then** the package SHALL use a placeholder version `0.0.0-local`
**And** a comment in the csproj SHALL indicate the version is overridden by CI

#### Scenario: Release notes metadata on NuGet.org
**Given** the template package is published to NuGet.org
**When** a user views the package page
**Then** a `PackageReleaseNotes` link SHALL point to `https://github.com/estebanjosse/mcp-server-template/releases`
**And** the link SHALL be clickable on the NuGet.org package page
