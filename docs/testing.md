# Testing

The repository uses C# and xUnit for unit, host integration and template tests.
Install the .NET 10 SDK, then run from the repository root:

```sh
dotnet test --configuration Release
```

No Python, PowerShell or globally installed template is required. The existing
CI workflow runs the same solution tests before building and smoke-testing the
Docker image.

## Test projects

- `tests/*.Application.Tests`, `*.Infrastructure.Tests` and `*.Mcp.Tests` test
  business logic, infrastructure and MCP adapters.
- `tests/*.Host.Http.Tests` test endpoint and authentication behavior in-process.
  Invalid authentication settings are also checked by launching the real HTTP
  host and requiring a failed startup with the precise validation diagnostic.
- `tests/*.Host.Stdio.Tests` start the actual stdio host as a child process.
  They check MCP initialization and discovery, prompts/resources, stdout purity
  and bounded shutdown. Sample-tool tests also verify echo data, division
  results and the precise division-by-zero business error.
- `validation/*.Template.Tests` test the template package itself. These are
  **repository-only tests**: they are not shipped in the package or added to
  generated projects.

Run one integration suite while developing:

```sh
dotnet test tests/McpServer.Template.Host.Http.Tests --configuration Release
dotnet test tests/McpServer.Template.Host.Stdio.Tests --configuration Release
dotnet test validation/McpServer.Template.Template.Tests --configuration Release
```

The stdio tests launch the real compiled application, without test-only service
registrations or logging overrides. A generic tool activation failure does not
satisfy the expected business-error assertion. Stdio stdout must contain only
protocol frames; console diagnostics belong on stderr.

## What the template tests verify

The template suite packs the repository's NuGet template and installs that
exact package into a fresh, isolated template-engine hive. It does not alter
the developer's normal `dotnet new` installation.

Tests cover:

- rejection of missing/invalid host selections before files are written;
- compatibility of legacy host flags, the `--host` selector and additive mixed
  selections;
- exact generated solution/project membership and optional sample content;
- the 12 combinations of HTTP/stdio/both hosts, tests on/off and sample tools
  on/off;
- building each valid combination and executing every expected generated test
  assembly when tests are included.

Generated test reports must be present, unique and contain executed tests with
no failures or skipped tests. A successful command with no tests is not enough.
This suite takes longer than the unit tests because it builds generated
solutions. Generated Docker images are not part of this matrix; CI separately
validates the repository's Docker image.

## Diagnostics

Integration tests retain process stdout/stderr and template-command evidence
under a unique temporary directory. Set `MCP_TEST_ARTIFACTS` to an external base
directory to collect these files in a chosen location. Do not put diagnostics
inside the repository or include secrets in test payloads.

To collect standard .NET test reports as well:

```sh
dotnet test --configuration Release --logger trx
```

TRX files are written to each test project's `TestResults` directory. CI retains
repository and generated-project TRX reports even on failure. If the test job
fails, a separate diagnostic artifact contains process logs, MCP request traces,
command metadata and the template package hash/inventory. Generated source,
build outputs and package binaries are not uploaded. Both artifacts have
14-day retention.

## Tests in generated projects

`--include-tests` includes application, infrastructure and MCP adapter tests,
plus integration tests for the selected hosts. Sample-tool tests are included
only with `--include-sample-tools`. Run `dotnet test` from the generated solution
root. Package-generation tests remain in this repository and do not recursively
run inside a generated project.
