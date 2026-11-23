# MCP Server Template

EasyMcp.McpServer.Template is a `dotnet new` template for quickly scaffolding clean, scalable, production-ready Model Context Protocol (MCP) servers in .NET 10.

This package is a **template**, not a runtime library: installing it adds a `mcp-server` template to your `dotnet new` list so you can generate full MCP server solutions (HTTP and/or stdio hosts, application layer, tests, and more).

## Existing-project migration

Updating the installed template does not update previously generated application source. The [MCP 2026-07-28 migration guide](migrations/mcp-2026-07-28.md) covers the optional SDK 1.3.0 to 2.4.0 transition, validation and rollback. See the [compatibility matrix](migrations/mcp-2026-07-28-compatibility.md) and [unreleased release notes](migrations/mcp-2026-07-28-release-notes.md) for tested paths and breaking changes.

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
| `--host` | | Select `http`, `stdio`, or `both`; required if neither host flag selects a host | No default |
| `--http-host` | `-hh` | Include HTTP host project | `false` |
| `--stdio-host` | `-sh` | Include Stdio host project | `false` |
| `--include-tests` | `-it` | Include test projects | `false` |
| `--include-sample-tools` | `-ist` | Include sample MCP tools | `false` |

> **Note:** Select at least one host using `--host http|stdio|both`, or the existing `--http-host` / `--stdio-host` flags. Both flag defaults remain `false`. With no host selected, the template engine reports `Missing mandatory option(s) ... '--host'` before writing files. Run `dotnet new mcp-server --help` to see the available choices.

The selector is additive: `--http-host --host stdio` includes both hosts. A `false` flag does not disable a host selected through `--host`. Existing valid flag-only commands remain supported.

```bash
# Equivalent to --http-host --stdio-host
dotnet new mcp-server --name Contoso.Mcp --host both
```

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

Runs on pushes to `main`, pull requests targeting `main`, and published releases:

1. **Test** - Restores, builds, and runs all tests
2. **Build Docker Image** - Builds the Docker image (no push on PRs)
3. **Publish to GHCR** - Pushes Docker images after the test and build jobs pass on `main` or on published version tags. This job does not run on pull requests. Merging into `main` triggers publication, including an update to `latest`.

The NuGet template does not include repository GitHub Actions workflows. Add your own CI to the generated application if needed.

### NuGet Publish Workflow (`.github/workflows/nuget-publish.yml`)

Runs when a GitHub Release is published with a version tag (e.g., `v1.0.0`):

1. **Extract Version** - Reads version from release tag (`refs/tags/v*.*.*`)
2. **Pack** - Creates the `.nupkg` template package with version from tag
3. **Publish** - Pushes to nuget.org using `NUGET_API_KEY` secret

This workflow is **excluded from generated projects** (template-specific).

### Publishing a New Version

1. Confirm merge and image publication are authorized before putting changes on `main`.
2. After separate release approval, confirm required CI passed on the exact commit you intend to tag. The NuGet workflow itself does not wait for the CI workflow. Create and push the approved tag for that commit without force. Before publishing, read back the remote tag and verify that it resolves to that exact commit (use the peeled commit for an annotated tag). Stop if the tag is missing or points elsewhere; do not retarget an existing release tag.
3. Publish using that verified tag. `--verify-tag` prevents the CLI from creating a missing tag; it does not check the tag's commit:
   ```bash
    gh release create "$APPROVED_RELEASE_TAG" \
      --verify-tag \
      --generate-notes
   ```
4. The NuGet Publish workflow runs automatically on `release: published`, then packs and publishes.

> Note: `McpServer.Template.csproj` uses a local placeholder version (`0.0.0-local`).
> CI overrides it at pack time with the version extracted from the release tag.

### Required Secrets

| Secret | Description |
|--------|-------------|
| `NUGET_API_KEY` | API key for nuget.org publishing |

### Optional: Environment Protection

Create a `nuget` environment in GitHub repository settings to add approval requirements before publishing.

## Verification

Run the repository-only C#/xUnit template suite:

```sh
dotnet test validation/McpServer.Template.Template.Tests --configuration Release
```

It packs and installs the actual NuGet template into a private hive, checks host
selection and generated project inventories, then builds and tests the 12
host/tests/sample combinations. It does not change your normal template
installation. See [Testing](testing.md) for the full solution command and
diagnostic artifact locations.

## Uninstalling

```bash
# From NuGet package
dotnet new uninstall EasyMcp.McpServer.Template

# From local source
dotnet new uninstall /path/to/mcp-server-template
```
