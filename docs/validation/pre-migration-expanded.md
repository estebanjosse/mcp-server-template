# Expanded pre-migration validation

## Scope and reference

This is pre-migration characterization, not implementation approval. Application code and the MCP SDK **1.3.0** remain unchanged from `9bccf55b056dc122dd1649bb88c1bcb62cad90b1` (and the original inspected source baseline). The new Python scripts validate that existing behavior without repairing it.

Assessment date: November 10, 2026. Raw outputs are retained outside the repository under execution ID `expanded-baseline-20261110`.

## Docker build and runtime: PASS in GitHub Actions

Hermes has the Docker CLI but no accessible daemon/socket, and this container cannot create an unprivileged user namespace. A local Docker build is therefore blocked by the execution environment, not classified as an application build failure.

Draft PR **#65** was opened solely to run the existing pull-request CI, without merging or publishing. CI run **42905885975**, head `9bccf55b056dc122dd1649bb88c1bcb62cad90b1`, completed successfully:

- Existing solution tests: PASS.
- Docker image build: PASS.
- Container `/health` readiness: PASS.
- Wrong/missing API key rejected with 401: PASS.
- Valid key accepted: PASS.
- Authentication mode `none` accepts unauthenticated access: PASS.
- Publish to GHCR: **SKIPPED**. No release or deployment was authorized.

See `https://github.com/estebanjosse/mcp-server-template/actions/runs/42905885975` for the exact CI execution. This result covers the repository Dockerfile, not every generated project's Dockerfile.

## Actual packed-template generation: 12 valid combinations PASS

The validator packed `EasyMcp.McpServer.Template.0.0.0-baseline.nupkg`, installed that exact file into an isolated template-engine hive, and generated every combination of:

- Host selection: HTTP only, stdio only, both.
- Tests: excluded or included.
- Sample tools: excluded or included.

All **12 valid combinations** passed project/solution inventory checks and Release builds. The six combinations including tests produced **411 passing tests**, with no failing/skipped tests. TRX files were checked against the expected test-project inventory to reject missing or duplicate reports.

Package SHA-256: `66ff8a85b1201bed014eac10d8e412f47016c52714862d9dfeecbfcf0f46a9e8`.

This is a locally packed pre-migration candidate, not a new NuGet release and not a rehearsal of the migration guide. The run records its exact package inventory and hash outside the repository.

## Existing defects and gaps kept visible

1. **No-host validation fails its expected rejection.** Generating with both host flags false succeeds, although onboarding says a host is required. The validation script intentionally returns a nonzero exit for this case; 12 successful builds do not make the whole matrix green.
2. **Stdio-only scaffolds retain an HTTP Dockerfile.** All four generated stdio-only combinations contain a Dockerfile referencing the excluded HTTP host. This is an inventory/source observation; a Docker build of those generated contexts has not been executed here.
3. **Packaged scaffolds do not contain the advertised CI workflow.** The packed inventory and all generated projects omit `.github/workflows/ci.yml`, while `docs/template.md` says it is included. This is separate from the successful CI of this repository.

No application, template-definition or Dockerfile fixes were made during characterization.

## Reproduce generation checks

With .NET 10 and Python 3.11+ available:

```bash
python3 -B scripts/validate_generated_projects.py --output /absolute/path/to/new-external-directory
python3 -B -m unittest discover -s scripts/tests -v
```

The output directory must be new and outside the repository. The script never deletes an existing directory, installs into a private template hive, keeps command logs/TRX/JSON results, and returns nonzero for failed requirements. Commands have bounded timeouts and builds run with one MSBuild worker and without persistent build servers.

The harness's **9 unit/negative-control tests pass**. Their synthetic TRX inputs are validator fixtures, not extra .NET application tests.

## Stdio end-to-end status

A separate strict stdin/stdout MCP validator is being exercised against the real unchanged host. Its protocol, invocation, resource/prompt and shutdown results are **PENDING**, not inferred from compilation or unit tests. Generated-host end-to-end results will be added separately.

## Boundaries

- Specification approval is still pending; no SDK/protocol migration has started.
- No claim is made about every external MCP client or production proxy configuration.
- Expected failures are retained for later scoped fixes, not suppressed to make the baseline green.
- The draft PR is not an approval to merge, release, publish or deploy.
