# MCP Server Template

A `dotnet new` template for quickly scaffolding Model Context Protocol (MCP) servers in .NET.

## Installation

### From NuGet (recommended)

```bash
dotnet new install McpServer.Template
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

### Basic Usage

Create a new MCP server with HTTP transport:

```bash
dotnet new mcp-server --name MyCompany.McpServer --http-host -o my-server
cd my-server
dotnet build
```

### Template Options

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

## Generated Project Structure

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

## Running the Server

### HTTP Host

```bash
cd src/MyCompany.McpServer.Host.Http
dotnet run
```

The server starts on `http://localhost:5000` by default.

### Stdio Host

```bash
cd src/MyCompany.McpServer.Host.Stdio
dotnet run
```

Use this for MCP clients that communicate via standard input/output.

## Adding MCP Tools

Create a new tool in `src/MyCompany.McpServer.Mcp/Tools/`:

```csharp
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MyCompany.McpServer.Mcp.Tools;

[McpServerToolType]
public sealed class MyTool
{
    [McpServerTool(Name = "my_tool")]
    [Description("Description of what my tool does")]
    public async Task<MyResponse> ExecuteAsync(
        [Description("Parameter description")] string input,
        CancellationToken cancellationToken = default)
    {
        // Implementation
        return new MyResponse(/* ... */);
    }
}
```

## Packaging the Template

### Building the NuGet Package

```bash
# Pack the template
dotnet pack -c Release

# The package is created at:
# bin/Release/McpServer.Template.<version>.nupkg
```

### Testing Locally

```bash
# Uninstall existing version
dotnet new uninstall McpServer.Template

# Install from local package
dotnet new install ./bin/Release/McpServer.Template.1.0.0.nupkg

# Verify installation
dotnet new list mcp-server
```

### Publishing to NuGet

```bash
dotnet nuget push ./bin/Release/McpServer.Template.1.0.0.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

## CI/CD Integration

The template includes a GitHub Actions workflow that:

1. Runs tests on every push and pull request
2. Builds and publishes the NuGet package on version tags (e.g., `v1.0.0`)

### Publishing a New Version

1. Update version in `Directory.Build.props`
2. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. The CI pipeline automatically publishes to NuGet

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
dotnet new uninstall McpServer.Template

# From local source
dotnet new uninstall /path/to/mcp-server-template
```
