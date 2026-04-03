## MODIFIED Requirements

### Requirement: Optional Content Flags

The template MUST expose boolean parameters that opt into sample tools and test projects without including them by default.

#### Scenario: Include sample tools on request
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host --include-sample-tools`  
**Then** the generated solution MUST contain the full generic sample tool set and its required supporting files  
**And** `.template.config/template.json` MUST include the sample-related source files, tests, and sample-facing documentation needed for the generated scaffold to build and run coherently

#### Scenario: Exclude sample tools by default
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host`  
**Then** the generated `Mcp` project MUST contain an empty `Tools/` directory with a placeholder to keep the folder committed  
**And** `.template.config/template.json` MUST exclude sample-only source files, sample tests, and sample-facing documentation so the generated scaffold remains minimal and internally consistent

#### Scenario: Include tests on request
**Given** the template package is installed  
**When** a developer runs `dotnet new mcp-server --http-host --include-tests`  
**Then** the solution MUST include the test projects mirrored from the repository  
**And** `dotnet test` MUST succeed against the generated solution