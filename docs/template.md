# MCP Server Template

EasyMcp.McpServer.Template is a `dotnet new` template for quickly scaffolding clean, scalable, production-ready Model Context Protocol (MCP) servers in .NET 8.

This package is a **template**, not a runtime library: installing it adds a `mcp-server` template to your `dotnet new` list so you can generate full MCP server solutions (HTTP and/or stdio hosts, application layer, tests, and more).

## Installation

### From NuGet (recommended)

```bash
dotnet new install EasyMcp.McpServer.Template
```

### From local source

```bash
# Clone or download the repository
git clone https://github.com/estebanjosse/mcp-server-template.git
cd mcp-server-template

# Install from local directory
dotnet new install .
```

## Usage

### Recommended quick start

Create a new MCP server that includes both HTTP and stdio hosts plus tests (mirrors the example from the root README):

```bash
dotnet new mcp-server \ 
    --name MyCompany.McpServer \ 
    --http-host \ 
    --stdio-host \ 
    --include-tests \ 
    -o my-server

cd my-server
dotnet build
```

Run the generated hosts:

```bash
# HTTP host
dotnet run --project src/MyCompany.McpServer.Host.Http

# Stdio host
dotnet run --project src/MyCompany.McpServer.Host.Stdio
```

### Template options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--name` | `-n` | Project name (replaces `McpServer.Template`) | Directory name |
| `--http-host` | `-hh` | Include HTTP host project | `false` |
| `--stdio-host` | `-sh` | Include Stdio host project | `false` |
| `--include-tests` | `-it` | Include test projects | `false` |
| `--include-sample-tools` | `-ist` | Include sample MCP tools | `false` |

> **Note:** At least one host option (`--http-host` or `--stdio-host`) is required.

### Examples

**HTTP server only:**
```bash
dotnet new mcp-server --name Contoso.Mcp --http-host
```

**Stdio server only:**
```bash
dotnet new mcp-server --name Contoso.Mcp --stdio-host
```

**Both transports with tests:**
```bash
dotnet new mcp-server --name Contoso.Mcp --http-host --stdio-host --include-tests
```

**Full scaffold with samples and tests:**
```bash
dotnet new mcp-server --name Contoso.Mcp --http-host --include-tests --include-sample-tools
```

## Generated project structure

```
MyCompany.McpServer/
├── MyCompany.McpServer.slnx          # Solution file
├── Directory.Build.props             # Shared build properties
├── Directory.Build.targets           # Shared build targets
├── src/
│   ├── MyCompany.McpServer.Contracts/      # DTOs and constants
│   ├── MyCompany.McpServer.Application/    # Business logic and ports
│   ├── MyCompany.McpServer.Infrastructure/ # Infrastructure implementations
│   ├── MyCompany.McpServer.Mcp/            # MCP tools, prompts, resources
│   ├── MyCompany.McpServer.Host.Http/      # HTTP host (if --http-host)
│   └── MyCompany.McpServer.Host.Stdio/     # Stdio host (if --stdio-host)
└── tests/                                   # Test projects (if --include-tests)
    ├── MyCompany.McpServer.Application.Tests/
    ├── MyCompany.McpServer.Infrastructure.Tests/
    ├── MyCompany.McpServer.Mcp.Tests/
    └── MyCompany.McpServer.Host.Http.Tests/ # (if --http-host)
```

## Packaging the template

### Building the NuGet Package

```bash
# Pack the template
dotnet pack -c Release

# The package is created at:
# bin/Release/EasyMcp.McpServer.Template.<version>.nupkg
```

### Testing Locally

```bash
# Uninstall existing version
dotnet new uninstall EasyMcp.McpServer.Template

# Install from local package
dotnet new install ./bin/Release/EasyMcp.McpServer.Template.1.0.0.nupkg

# Verify installation
dotnet new list mcp-server
```

### Publishing to NuGet

```bash
dotnet nuget push ./bin/Release/EasyMcp.McpServer.Template.0.0.2-preview.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

## CI/CD integration

The repository uses two separate GitHub Actions workflows:

### CI Workflow (`.github/workflows/ci.yml`)

Runs on every push to `main` and pull requests:

1. **Test** - Restores, builds, and runs all tests
2. **Build Docker Image** - Builds the Docker image (no push on PRs)
3. **Publish to GHCR** - Pushes Docker image to GitHub Container Registry (main branch only)

This workflow is **included in generated projects** from the template.

### NuGet Publish Workflow (`.github/workflows/nuget-publish.yml`)

Runs only on version tags (e.g., `v1.0.0`):

1. **Wait for CI** - Waits for the CI workflow's `Test` job to succeed
2. **Pack** - Creates the `.nupkg` template package with version from tag
3. **Publish** - Pushes to nuget.org using `NUGET_API_KEY` secret

This workflow is **excluded from generated projects** (template-specific).

### Publishing a New Version

1. Ensure all changes are committed and pushed to `main`
2. Create and push a version tag:
   ```bash
   git tag v1.2.0
   git push origin v1.2.0
   ```
3. The CI workflow runs tests on the tag
4. The NuGet Publish workflow waits for tests, then packs and publishes

### Required Secrets

| Secret | Description |
|--------|-------------|
| `NUGET_API_KEY` | API key for nuget.org publishing |

### Optional: Environment Protection

Create a `nuget` environment in GitHub repository settings to add approval requirements before publishing.

## Verification

Run the template verification script to test all flag combinations:

```powershell
./scripts/Test-Template.ps1
```

Options:
- `-OutputPath <path>`: Custom output directory for test projects
- `-SkipCleanup`: Keep test directories after the run

## Uninstalling

```bash
# From NuGet package
dotnet new uninstall EasyMcp.McpServer.Template

# From local source
dotnet new uninstall /path/to/mcp-server-template
```
