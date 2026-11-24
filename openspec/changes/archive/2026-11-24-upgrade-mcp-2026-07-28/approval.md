# Implementation approval checklist

**Review date:** November 16, 2026. **Status:** migration scope accepted in principle with the PR #65 review correction below. Tasks 2.1 and 2.2 were authorized; task 2.3 was authorized after a successful review of 2.2. Tasks 2.4 and 2.5 were separately authorized. Task 2.5 was initially local-only; its push was subsequently authorized along with implementation and push of task 2.6. Task 2.7 onward requires separate approval.

The study branch incorporates `main` at `6436000dab2c56ae1d53e3299cf27ef526657181` (PR #66). The published template 1.4.0 and the pre-upgrade baseline use SDK 1.3.0. This branch now uses SDK 2.4.0 after task 2.3. The July proposal on `spec/upgrade-to-new-mcp-protocol-spec` is reference material, not a second implementation plan. Its metrics contract is incorporated here; its unconditional builder refactor is not.

## Decisions required before implementation

1. **Target:** adopt protocol 2026-07-28 and aligned official C# SDK 2.4.0 packages on .NET 10. Rechecked November 16 against protocol `latest`, the SDK latest stable release and published NuGet indexes. Reconcile only dependency floors needed by that SDK; do not merge unrelated dependency updates.
2. **New-project HTTP policy:** explicitly configure `SessionMode = HttpServerSessionMode.Stateless` on `/mcp`. No public mode-selection flag, persistent sessions or legacy `/sse` transport in the default scaffold. SSE responses to POST remain distinct from legacy SSE endpoints.
3. **Compatibility boundary:** qualify modern 2026-07-28 plus initialize-handshake revisions 2025-11-25 and 2025-06-18 on HTTP and stdio for the scaffold's capabilities. Historical HTTP clients must work without persistent sessions. Hybrid and stateful compatibility modes are out of scope for the runtime, tests and migration guide. Existing deployments that depend on sessions must migrate their clients and application assumptions before adopting the upgraded host.
4. **Operational breaking change:** replace `mcp_sessions_active` with `mcp_requests_in_flight`, remove the old gauge rather than keeping a misleading alias, and document dashboard/alert migration. Count HTTP requests admitted past the existing authentication gate, including discovery and requests subsequently rejected by the SDK. Decrement on every exit, including cancellation and failure; do not count rejected credentials or monitoring routes.
5. **Acceptance scope:** retain and extend C#/xUnit validation from #66, run the full 12-case packed-template matrix, exercise real modern/down-level hosts, and rehearse the voluntary guide on published template 1.4.0 with customized business code. Retire superseded Python characterization scripts at task 3.1 only after equivalent passing replacement coverage is demonstrated, including generated hosts with tests excluded; no second maintained test workflow.
6. **Scope exclusions:** no business-layer redesign, unconditional MCP builder refactor, OAuth/JWT implementation, auth-mode expansion, Tasks/Apps/Skills/MRTR showcases, positive caching feature, unrelated Docker/package fixes, merge, release or deployment. Preserve the current authentication gate and its documented limitations. A demonstrated additional blocker requires a separate scoped decision.
7. **Client evidence — selected:** MCP Inspector 2.13.0, in addition to the official C# SDK. Qualification belongs to task 3.3 and remains NOT RUN. No paid service or external credentials are authorized.
8. **Execution cadence — approved for task 2.1:** Esteban allowed the agent to commit and push this checkpoint instead of leaving the commit to the user as specified by `custom-spec-driven/schema.yaml`. The one-task stop rule remains. Esteban separately authorized the reviewed sequence through task 2.3. A push is not authorization to start task 2.4, merge or publish.

## Later decisions, not prerequisites to the first implementation task

- Exact template release number and release date: decide before publication; do not copy the SDK version or imply a maintenance commitment for older generated applications.
- Merge, archival, release, package/image publication and deployment: require separate authorization after exact-head CI and acceptance review.

## Known limits retained

- Published template 1.4.0 and current `main` are different baselines; the voluntary guide must account for the stdio prerequisite fixes in #66.
- The historical study scripts remain in the branch for now and are not the maintained acceptance path. Current commands are in `docs/testing.md`.
- Stdio-only generated Docker context, sample-free welcome content and third-party notice inventory require independent assessment; the full generated build/test matrix does not prove generated Docker images run.
- This planning checkpoint does not qualify any SDK 2.4.0 runtime binary or a production migration.
