# Pre-migration regression tests (RED checkpoint)

This branch isolates confirmed defects before any protocol or SDK migration.
It starts from `main` at `fc2b908280e530bf3bdc20bf3abe6ed346ed1d64`,
not from the migration proposal branch. Application source, .NET tests, template
configuration, Dockerfile and dependency versions are unchanged. The MCP SDK
remains 1.3.0. No application fixes are included or authorized by this checkpoint.

## Confirmed defects

- Stdio sample tools cannot activate because `IMcpMetricsRecorder` is not
  registered: echo and normal division fail, and division by zero returns an
  activation failure instead of the expected business error.
- Console logs contaminate stdout, which must contain only protocol frames.
- The packaged template accepts both hosts disabled instead of rejecting the
  configuration. The existing template specification requires at least one host.

These are three root defects, not five independent bugs. Other earlier audit
concerns are not claimed covered or reproduced by this targeted test suite.

## Reproduce

Requirements: Linux, Python 3.11+ and .NET 10. No Python packages are required.
Run from the repository root; choose a fresh evidence path outside the checkout.

```sh
dotnet build src/McpServer.Template.Host.Stdio --configuration Release --disable-build-servers
export MCP_REGRESSION_OUTPUT="$HOME/mcp-regression-evidence/run-001"
python3 -B scripts/integration_tests/test_pre_migration_regressions.py -v
```

Individual suites can run as `StdioRegressions` or `TemplateRegressions` before
`-v`. Template setup packs the real NuGet package, installs it in a private hive,
and runs `dotnet new`; it does not install into the user's normal template hive.
The stdio suite starts the real built host without logging or DI overrides.

Local execution on November 11, 2026: **6 tests executed, 5 assertion failures,
1 pass, no setup errors**. The four stdio failures are echo, normal division,
division-by-zero semantics and stdout purity (168 invalid stdout frames). The
no-host generation test fails because `dotnet new` exits 0. The remaining
protocol/discovery/resources/prompts checks and bounded EOF shutdown pass.
Build: 0 warnings and 0 errors. Harness-only suites: 12 template-validator tests
and 13 stdio-validator tests pass. These passing synthetic harness tests are not
application regression results.

## CI and approval boundary

`Pre-migration regressions` runs on pull requests targeting `main`, including
Draft PRs. Independent stdio and template jobs use `fail-fast: false` so both
failures remain visible. Neither is skipped, marked expected-failure, nor masked
with `continue-on-error`. Evidence is uploaded even when assertions fail, with
14-day retention. A separate job verifies the validation harness itself.
The existing CI remains intact; no image/package publication is added.

The imported validators originate from migration-study checkpoint `eac1099`;
they are test infrastructure, not protocol migration code. The full 12-case
matrix script is available but is not run by this targeted workflow. This
checkpoint does not revalidate all generated hosts or generated Docker images.

Stop here for review: implementing fixes, upgrading the SDK/protocol, merging,
releasing and deploying remain separate steps requiring approval. The Draft PR
must remain RED until the confirmed defects are fixed in subsequent authorized
commits; do not change assertions merely to turn CI green.
