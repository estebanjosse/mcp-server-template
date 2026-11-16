## 1. Study checkpoint — implementation not approved

- [x] 1.1 Verify dated stable protocol/SDK targets against official sources and published package metadata; rechecked November 16, 2026.
- [x] 1.2 Inspect source and map behavioral, dependency and generated-project impact, incorporating main `6436000` and #66.
- [x] 1.3 Draft compatibility decisions, migration guides, explicit metrics delta and deferred showcase scope in English.
- [ ] 1.4 Obtain Esteban's decisions from approval.md, exact external client targets and explicit authorization for the first implementation task.

## 2. Authorized implementation only — blocked on 1.4

Execute one task at a time, commit/push the coherent checkpoint, verify its remote SHA, and stop for user review. No subsequent task, merge or release is implied by a checkpoint push.

- [ ] 2.1 Record an exact-head SDK 1.3.0 baseline using the maintained C#/xUnit suites from #66, including real stdio semantics and the full packed-template matrix; report regressions separately from historical November 10 failures.
- [ ] 2.2 Extend C# fixtures with modern/down-level cases and pin exact status/error/body expectations from versioned upstream contracts; document expected pre-upgrade failures without weakening existing coverage. Map replaced Python coverage before retiring its superseded scripts in this same validation checkpoint.
- [ ] 2.3 Upgrade aligned MCP packages to 2.4.0, reconcile required direct/transitive floors and record resolved dependency diagnostics; do not upgrade unrelated packages or refactor registration without demonstrated need.
- [ ] 2.4 Configure explicit stateless HTTP on /mcp and qualify modern discovery/direct requests plus 2025-11-25 and 2025-06-18 initialize/capability operations with exact wire assertions.
- [ ] 2.5 Qualify real stdio modern/down-level operations, sample/no-sample capability handling, exact tool results/errors, cancellation, stdout purity and bounded shutdown/drains.
- [ ] 2.6 Implement the metrics-observability delta, testing concurrency, error/cancellation cleanup, disabled mode and authentication/monitoring boundaries; document the breaking gauge rename.
- [ ] 2.7 Qualify revision-aware schema/result/cache/resource-error fixtures and HTTP authentication continuity, without adding OAuth, positive caching or new runtime features.

## 3. Consumer and release qualification — no publication authorized

- [ ] 3.1 Pack/install the candidate in an isolated hive; validate all 12 host/sample/test combinations, host-selector/rejection contracts, unique complete TRX reports and real generated-host operations including tests-off cases; retain minimal evidence outside the repository.
- [ ] 3.2 Rehearse the voluntary guide on published template 1.4.0 with customized business code, applying its stdio prerequisites and preserving user changes; test coherent rollback.
- [ ] 3.3 Verify the agreed exact external clients and representative proxy/header/timeout/cancellation behavior; report unavailable scenarios as NOT RUN rather than broad compatibility.
- [ ] 3.4 Finalize guides, compatibility matrix, breaking release notes, package inventory and third-party attribution; reconcile any additionally approved existing-capability deltas and leave unrelated packaging/Docker issues separately scoped.
- [ ] 3.5 Independently review the exact candidate head and verify its CI; obtain separate merge/release/archival authorization and choose the template release number before publication.

## 4. Separate future work

- [ ] 4.1 Obtain separate approval for feature showcases, optional hybrid runtime/configuration, expanded authentication or other extensions; none is authorized by this plan.
