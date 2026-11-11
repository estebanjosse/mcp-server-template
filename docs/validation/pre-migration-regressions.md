# Pre-migration regressions: RED baseline and fixes

This branch isolates confirmed defects before any protocol or SDK migration.
It starts from `main` at `fc2b908280e530bf3bdc20bf3abe6ed346ed1d64`, not the
migration proposal branch. The MCP SDK remains **1.3.0**. No dependency upgrade,
merge, release or deployment is included.

## Original RED checkpoint

At `315eab2`, application source, existing .NET tests, template configuration,
Dockerfile and dependency versions were unchanged. Local and GitHub Actions
execution on November 11, 2026 produced **6 integration tests: 5 assertion
failures, 1 pass, no setup errors**. CI regression run: `42990859950`.

Three root defects were exposed:

- Stdio sample tools could not activate because `IMcpMetricsRecorder` was not
  registered: echo and normal division failed, and division by zero returned an
  activation failure instead of the business error.
- Console logs contaminated stdout (168 invalid frames).
- The real packaged template accepted both hosts disabled instead of rejecting
  generation. The existing template specification requires at least one host.

Initialization, discovery, prompts, resources and bounded EOF shutdown passed.
These were three root defects, not five independent bugs. Other audit concerns
are not claimed covered by these targeted tests.

## Corrective checkpoints

- `09e9ca6`: register the existing no-op metrics recorder in the stdio host.
  Echo and normal division become GREEN together because they share one cause.
  The existing .NET suite passes (84 tests). HTTP registration is unchanged.
- `361bb0a`: route all enabled console log levels to stderr, not stdout.
  Stdout purity becomes GREEN; diagnostics remain available.
- `5863a54`: correct a validator false RED uncovered after repairing DI.
  SDK 1.3.0 wraps the business error as
  `An error occurred invoking 'calc_divide': Division by zero is not allowed`.
  The bare-only expectation was wrong. The validator now accepts the exact bare
  or exact SDK-wrapped message; generic activation errors, a wrong tool name
  and appended text still fail. A new harness test was observed RED then GREEN.
  No application behavior was changed in this checkpoint.
- `8318fb6`: reject no-host generation before files are written, using a real
  `--host http|stdio|both` choice that is required when neither legacy flag
  selects a host. Existing bare/explicit `--http-host` and `--stdio-host`
  commands remain supported. Selections are additive and determine both source
  directories and solution/test project membership. This is not an ignorable
  boolean sentinel that can bypass the host requirement.

The no-host tests retain their nonzero-exit and no-partial-solution assertions.
The diagnostic check additionally recognizes the engine's actual word
`mandatory`, alongside `required`, `at least` and `constraint`.

## Verification at `8318fb6`

Executed locally on November 11, 2026 with .NET SDK 10.0.404:

- Release solution build: **0 warnings, 0 errors**.
- Existing .NET suite: **84 passed**, no failures or skips.
- Real integration suite: **9 passed** (5 stdio, 4 template tests).
  Template coverage includes omitted host options, explicit false/false,
  invalid choices, and 11 legal flag/choice combinations with sample tools and
  tests included. Exact solution project membership is checked.
- Harness suites: **12 template-validator tests and 14 stdio-validator tests
  passed**. These are synthetic harness checks, not application test counts.
- Packed-template matrix in a private hive: **12/12 valid combinations build**,
  **411 generated .NET tests pass**, and no-host generation is rejected.
- All **8 generated stdio hosts pass** the real protocol validator: four
  sample-enabled hosts pass 13 checks each, four sample-free hosts pass 10 each.
  All exit 0 on EOF; no transport, reader or drain failures were recorded.

The full matrix uses the legacy host flags. The additional choice combinations
are generated and their inventories checked by the targeted suite; they are
not a second exhaustive build matrix. Generated Docker images are not tested.
Local logs/TRX/protocol transcripts are archived outside the public repository.
GitHub CI results must be checked separately for the exact current PR head;
local success alone is not a claim that CI has completed.

## Reproduce

Requirements: Linux, Python 3.11+ and .NET 10. No Python packages are required.
Run from the repository root; choose fresh evidence paths outside the checkout.

```sh
dotnet build --configuration Release --disable-build-servers
dotnet test --configuration Release --no-build
export MCP_REGRESSION_OUTPUT="$HOME/mcp-regression-evidence/run-001"
python3 -B scripts/integration_tests/test_pre_migration_regressions.py -v
python3 -B -m unittest discover -s scripts/tests -v
python3 -B scripts/validate_stdio.py --self-test
python3 -B scripts/validate_generated_projects.py \
  --output "$HOME/mcp-regression-evidence/matrix-001"
```

Individual integration suites can run as `StdioRegressions` or
`TemplateRegressions` before `-v`. Template setup packs the actual NuGet
package, installs it in a private hive and executes `dotnet new`; it does not
modify the user's normal hive. Stdio validation launches the real built host
without test-only logging or DI overrides. The generated matrix results list
paths of built DLLs for repeatable `validate_stdio.py` invocations.

## CI and delivery boundary

`Pre-migration regressions` runs on PRs targeting `main`, including Draft PRs.
Independent stdio and template jobs use `fail-fast: false`; neither is marked
expected-failure nor masked with `continue-on-error`. Evidence uploads run even
on failure, with 14-day retention. A separate job tests the harness itself.
The existing test/Docker CI is unchanged; no publication path is added.
The full 12-case matrix is a separate local verification, not part of this
lightweight regression workflow.

Fixes were explicitly authorized after the RED checkpoint. The PR remains a
Draft; merging, releasing, deploying and protocol/SDK migration remain separate
approval gates. Preserve the historical RED evidence rather than rewriting it
as a successful run.
