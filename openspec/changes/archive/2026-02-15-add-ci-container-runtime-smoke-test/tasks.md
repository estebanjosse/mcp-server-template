## 1. CI workflow runtime smoke test

- [x] 1.1 Update the Docker build job in `.github/workflows/ci.yml` to build a runnable local image (`load: true`) with a deterministic CI tag.
- [x] 1.2 Add a step to start the built image as a container with explicit port mapping for the HTTP host.
- [x] 1.3 Add a bounded readiness loop that probes `GET /health` and marks the job failed when timeout is reached.

## 2. Failure handling and diagnostics

- [x] 2.1 Add fail-fast behavior when the runtime validation container exits before readiness is reached.
- [x] 2.2 Add failure-path diagnostics steps that capture `docker logs` and `docker inspect` output.
- [x] 2.3 Add cleanup steps to stop/remove the test container in all outcomes.

## 3. Verification and regression safety

- [x] 3.1 Validate CI passes with a known-good Docker/runtime combination.
- [x] 3.2 Validate CI fails with intentional runtime mismatch and provides actionable diagnostics in logs.
- [x] 3.3 Ensure existing test and publish behavior remains unchanged outside the new runtime smoke-test gate.
