# Implementation approval checklist

**Review date:** November 16, 2026. **Status:** proposed decisions, not implementation approval.

The study branch incorporates `main` at `6436000dab2c56ae1d53e3299cf27ef526657181` (PR #66). The SDK remains 1.3.0; this checkpoint only updates planning and documentation. The July proposal on `spec/upgrade-to-new-mcp-protocol-spec` is reference material, not a second implementation plan. Its metrics contract is incorporated here; its unconditional builder refactor is not.

## Decisions required before implementation

1. **Target:** adopt protocol 2026-07-28 and aligned official C# SDK 2.4.0 packages on .NET 10. Rechecked November 16 against protocol `latest`, the SDK latest stable release and published NuGet indexes. Reconcile only dependency floors needed by that SDK; do not merge unrelated dependency updates.
2. **New-project HTTP policy:** explicitly configure `SessionMode = HttpServerSessionMode.Stateless` on `/mcp`. No public mode-selection flag, persistent sessions or legacy `/sse` transport in the default scaffold. SSE responses to POST remain distinct from legacy SSE endpoints.
3. **Compatibility boundary:** qualify modern 2026-07-28 plus initialize-handshake revisions 2025-11-25 and 2025-06-18 on HTTP and stdio for the scaffold's capabilities. Historical HTTP clients must work without persistent sessions. Hybrid mode is optional advanced consumer guidance, not a shipped mode, a default or a blanket guarantee for old clients. Authenticated hybrid-session fixtures are conditional on separately approving that runtime surface.
4. **Operational breaking change:** replace `mcp_sessions_active` with `mcp_requests_in_flight`, remove the old gauge rather than keeping a misleading alias, and document dashboard/alert migration. Count HTTP requests admitted past the existing authentication gate, including discovery and requests subsequently rejected by the SDK. Decrement on every exit, including cancellation and failure; do not count rejected credentials or monitoring routes.
5. **Acceptance scope:** retain and extend C#/xUnit validation from #66, run the full 12-case packed-template matrix, exercise real modern/down-level hosts, and rehearse the voluntary guide on published template 1.4.0 with customized business code. Retire superseded Python characterization scripts within the authorized validation task after establishing replacement coverage; no second maintained test workflow.
6. **Scope exclusions:** no business-layer redesign, unconditional MCP builder refactor, OAuth/JWT implementation, auth-mode expansion, Tasks/Apps/Skills/MRTR showcases, positive caching feature, unrelated Docker/package fixes, merge, release or deployment. Preserve the current authentication gate and its documented limitations. A demonstrated additional blocker requires a separate scoped decision.
7. **Client evidence:** choose exact external clients/versions to qualify beyond the official C# SDK. Proposed minimum is the pinned C# SDK plus one actual client selected by Esteban; clients unavailable in this environment remain NOT RUN, not implicitly supported. No paid service or external credentials are authorized by this checklist.
8. **Execution cadence:** implement one OpenSpec task at a time, commit and push each coherent checkpoint, then stop for review before the next task. Push is not acceptance or permission to merge. Choose the first task explicitly when approving implementation.

## Later decisions, not prerequisites to the first implementation task

- Exact template release number and release date: decide before publication; do not copy the SDK version or imply a maintenance commitment for older generated applications.
- Merge, archival, release, package/image publication and deployment: require separate authorization after exact-head CI and acceptance review.

## Known limits retained

- Published template 1.4.0 and current `main` are different baselines; the voluntary guide must account for the stdio prerequisite fixes in #66.
- The historical study scripts remain in the branch for now and are not the maintained acceptance path. Current commands are in `docs/testing.md`.
- Stdio-only generated Docker context, sample-free welcome content and third-party notice inventory require independent assessment; the full generated build/test matrix does not prove generated Docker images run.
- This planning checkpoint does not qualify any SDK 2.4.0 runtime binary or a production migration.
