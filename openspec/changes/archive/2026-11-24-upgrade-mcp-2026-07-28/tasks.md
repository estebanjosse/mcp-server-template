## 1. Study checkpoint — implementation not approved

- [x] 1.1 Verify dated stable protocol/SDK targets against official sources and published package metadata; rechecked November 16, 2026.
- [x] 1.2 Inspect source and map behavioral, dependency and generated-project impact, incorporating main `6436000` and #66.
- [x] 1.3 Draft compatibility decisions, migration guides, explicit metrics delta and deferred showcase scope in English.
- [x] 1.4 Obtain execution approval for task 2.2 and conditional approval for task 2.3 after a successful review.

## 2. Baseline completed; migration implementation still gated

Esteban authorized task 2.1 only and explicitly allowed the agent to commit/push its checkpoint instead of leaving the commit to the user. This limited exception preserves the one-task stop and all remaining approval gates. Esteban subsequently authorized task 2.2 and allowed task 2.3 after a successful review of 2.2. Tasks 2.4 and 2.5 were subsequently authorized. After the local 2.5 checkpoint, Esteban authorized its push and the task 2.6 implementation and push. Do not start task 2.7 or later without separate approval.

- [x] 2.1 Record an exact-head SDK 1.3.0 baseline using the maintained C#/xUnit suites from #66, including real stdio semantics and the full packed-template matrix; [evidence and limits](baseline.md).
- [x] 2.2 Extend C# fixtures with modern/down-level cases and pin exact status/error/body expectations from versioned upstream contracts; document expected pre-upgrade failures without weakening existing coverage. Map replaced Python coverage without deleting the scripts yet; their retirement is gated on equivalent passing real-host coverage in 3.1, including generated hosts with tests excluded.
- [x] 2.3 Upgrade aligned MCP packages to 2.4.0, reconcile required direct/transitive floors and record resolved dependency diagnostics; do not upgrade unrelated packages or refactor registration without demonstrated need.
- [x] 2.4 Configure explicit stateless HTTP on /mcp and qualify modern discovery/direct requests plus 2025-11-25 and 2025-06-18 initialize/capability operations with exact wire assertions.
- [x] 2.5 Qualify real stdio modern/down-level operations, sample/no-sample capability handling, exact tool results/errors, cancellation, stdout purity and bounded shutdown/drains.
- [x] 2.6 Implement the metrics-observability delta, testing concurrency, error/cancellation cleanup, disabled mode and authentication/monitoring boundaries; document the breaking gauge rename.
- [x] 2.7 Qualify revision-aware schema/result/cache/resource-error fixtures and HTTP authentication continuity, without adding OAuth, positive caching or new runtime features.

## 3. Consumer and release qualification — no publication authorized

- [x] 3.1 Pack/install the candidate in an isolated hive; validate all 12 host/sample/test combinations, host-selector/rejection contracts, unique complete TRX reports and real generated-host operations including tests-off cases; retain minimal evidence outside the repository. Retire superseded Python scripts only after equivalent passing replacement coverage is demonstrated, including generated hosts with tests excluded.
- [x] 3.2 Rehearse the voluntary guide on published template 1.4.0 with customized business code, applying its stdio prerequisites and preserving user changes; test coherent rollback.
- [x] 3.3 Verify the agreed exact external clients and representative proxy/header/timeout/cancellation behavior; report unavailable scenarios as NOT RUN rather than broad compatibility.
  - Verified on `72a39206`: official C# SDK 2.4.0 and MCP Inspector CLI 2.13.0 over loopback HTTP, directly and through NGINX 1.26.3. Header forwarding, pre-response timeout, and server-side request cancellation were exercised. NOT RUN: Inspector stdio or browser UI, TLS/HTTP2, production topology, other proxies, and timeout after SSE response headers.
- [x] 3.4 Finalize guides, compatibility matrix, breaking release notes, package inventory and third-party attribution; reconcile any additionally approved existing-capability deltas and leave unrelated packaging/Docker issues separately scoped.
- [ ] 3.5 Independently review the exact candidate head and verify its CI; obtain separate merge/release/archival authorization and choose the template release number before publication.
  - Archive handoff: independent review, corrective reviews, and PR CI run `45257420799` verified candidate `4e3f0a56f6d476d813a1193daa4fb58ea8343471`. CI passed tests and Docker build/smoke checks; GHCR publication was skipped. These results do not cover the later archive commit.
  - Archival is authorized before maintainer review. Maintainer review, merge, and publication remain pending. The maintainer plans to publish template `v2.0` after merge. Before merge, check notices in the image intended for GHCR and account for the `main` workflow updating `latest`; before publication, verify CI and the remote tag against the exact release commit.

## 4. Separate future work

- [ ] 4.1 Obtain separate approval for feature showcases, expanded authentication or other extensions; none is authorized by this plan.
