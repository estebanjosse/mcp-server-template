# Pre-migration build and test baseline

## Scope

Executed on November 10, 2026, before any protocol or SDK migration.

- Tested commit: `394e2a37423621a00e71a26ede8369809557bce5`.
- Branch: `docs/mcp-2026-07-28-migration-study`.
- Runtime source, test source and build inputs are unchanged from the inspected baseline `fc2b908280e530bf3bdc20bf3abe6ed346ed1d64`.
- Existing MCP SDK version: **1.3.0**; target framework: **net10.0**.
- Environment: Debian 13, Linux x64; .NET SDK **10.0.404**, .NET / ASP.NET Core runtime **10.0.15**.
- Configuration: **Release**; solution: `McpServer.Template.slnx`.

## Executed commands and results

```bash
dotnet restore McpServer.Template.slnx
dotnet build McpServer.Template.slnx --configuration Release --no-restore
dotnet test McpServer.Template.slnx --configuration Release --no-build --no-restore \
  --logger 'trx;LogFilePrefix=baseline' --results-directory <external-evidence>/test-results
```

- Restore: **PASS**.
- Build: **PASS**, **0 warnings**, **0 errors**.
- Tests: **84 passed**, **0 failed**, **0 skipped**, across all four test projects.
  - Application: 24 passed.
  - Infrastructure: 7 passed.
  - MCP adapters: 20 passed.
  - HTTP host: 33 passed.
- Counts were read from four TRX files and checked against the solution's test-project inventory.
- The working tree remained clean and the tested commit unchanged after execution.

The runner emitted a non-fatal `VSTEST_ARTIFACTS_DIRECTORY environment variable was not found` notice for each test project and used its fallback directory. This is a test-tooling observation, not a compiler warning or a failing test. No package or test-runner change was made.

## Evidence and limits

See the [expanded baseline](pre-migration-expanded.md) for the subsequent Docker, packed-template and real stdio checks, including reproduced failures. The limits below describe this original build/test run only.

Raw restore/build/test logs, SDK information, machine-readable results and four TRX reports are retained outside the repository under execution ID `baseline-20261110T212101Z`.

This validates the existing solution's build and test suite only. It does **not** validate:

- The proposed SDK 2.4.0 / MCP 2026-07-28 migration.
- Real MCP interoperability beyond what the existing tests cover.
- A separate end-to-end stdio client conversation or the suspected stdio dependency-registration issue.
- Packed-template installation and generated-project combinations, or rehearsal of the draft migration guide.
- Docker image build/runtime, external clients, proxies or production deployment.

Implementation remains pending specification approval. Baseline success is not migration approval or permission to merge, release or deploy.
