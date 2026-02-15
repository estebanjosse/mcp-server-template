## Why

The current CI pipeline validates Docker image build success but does not validate runtime startup, so runtime/base-image incompatibilities can pass CI and only fail when running the container. Adding runtime validation now is necessary to make dependency update PRs (including Docker image bumps) safe by default.

## What Changes

- Add CI requirements to run the built Docker image in workflow jobs and verify it starts successfully.
- Add CI requirements to probe the HTTP host health endpoint from the running container and fail on timeout or startup crash.
- Add CI requirements to publish container diagnostics (logs and inspect output) when runtime validation fails.
- Keep dependency update visibility (including major Docker base-image updates) while enforcing runtime safety gates in CI.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `ci-pipeline`: Extend Docker validation requirements from build-only checks to runtime startup and health verification of the built container image.

## Impact

- Affected specs: `openspec/specs/ci-pipeline/spec.md` (new/updated requirement scenarios for container runtime validation).
- Affected systems: GitHub Actions workflow for CI (`.github/workflows/ci.yml`) and Docker-based validation flow.
- Operational impact: Dependabot Docker PRs remain visible, but incompatible runtime updates are blocked earlier in CI.
- Breaking changes: None.
