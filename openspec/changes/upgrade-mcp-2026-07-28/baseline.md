# Task 2.1 — SDK 1.3.0 baseline

- Tested PR head: `3a108005781fc14f516b22b704c5e4e54f24e5cc` (draft PR #65), including main `6436000dab2c56ae1d53e3299cf27ef526657181` (#66). This task records existing execution evidence; it changes no runtime, dependency, test, package or CI configuration.
- Target framework: .NET 10; MCP SDK packages: 1.3.0. Local .NET SDK: 10.0.404.
- [GitHub Actions run 43949862153](https://github.com/estebanjosse/mcp-server-template/actions/runs/43949862153), November 16, 2026: restore, Release build and tests passed. The repository Docker image built, started and passed `/health` and API-key/no-auth smoke checks. GHCR publication was skipped.

## Verified test evidence

The CI `test-results` artifact was checked against the expected assemblies: six repository reports (176 tests, including 40 HTTP, 47 stdio and 38 template-validation tests) and 26 generated assembly reports (591 tests). Each report contained nonzero executed tests with matching total/executed/passed counts and no failures, skips or aborts. Local results corroborated the counts in separate source and template runs; the initial combined local invocation hit the terminal deadline, and the dedicated template rerun completed successfully.

All 12 packed-template combinations built: HTTP only, stdio only and both, with tests and samples independently enabled or disabled. The six tests-enabled combinations executed every expected assembly. The suite also checked invalid host rejection before output, legacy and current host selectors, and generated project inventory.

The real stdio suite checks initialization and advertised capabilities, sample tool DTOs and exact division-by-zero error, prompts/resources, protocol-only stdout, and bounded EOF shutdown and stream drains. The November 10 missing-recorder, stdout-contamination and no-host failures are historical and are not reproduced by the current regression suite after #66.

## Limits

This is a C# SDK 1.3.0 baseline, not qualification of the SDK 2 migration, MCP Inspector, generated Docker images, or live invocation against generated scaffolds with tests disabled. Inspector 2.13.0 is selected for later task 3.3 and was not run. Task 2.2 and all later work remain unauthorized.
