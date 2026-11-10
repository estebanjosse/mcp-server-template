# Change: Upgrade to MCP 2026-07-28 and the official C# SDK 2.4.0

## Status

Impact study and documentation draft only. Runtime implementation, merge, release and deployment are not authorized. Targets were verified on November 10, 2026; recheck before implementation. See [design](design.md) and the [draft user migration guide](../../../docs/migrations/mcp-2026-07-28.md).

## Why

The published template 1.4.0 embeds SDK 1.3.0, whose newest protocol revision is 2025-11-25. The latest stable protocol and SDK introduce a different negotiation/session model, security fixes and new extension boundaries. A package-only upgrade could change behavior without a compile error and leave existing generated projects behind.

## What Changes

- Propose SDK 2.4.0 for all MCP packages and reconcile its actual published dependency floors while retaining .NET 10.
- **BREAKING**: make HTTP session policy explicit. Recommend stateless for newly generated projects, with a documented hybrid transition for existing session-dependent servers; do not select stateful-only as the modern-protocol path.
- Qualify modern discovery/per-request metadata and down-level initialize flows separately over HTTP and stdio.
- Update transport/error/schema/cache assertions, SDK-dependent examples, protocol documentation and packaging checks.
- Supply an English, non-destructive migration guide for existing generated applications, including compatibility choices, security caveats and rollback.
- Record a separate, deferred feature-showcase backlog. Core migration approval does not authorize new Tasks, Apps, Skills or interactive examples.

## Capabilities

### New Capabilities

- `mcp-protocol-compatibility`: version-aware transport behavior, supported client matrix, dependency alignment and wire-level acceptance evidence.
- `generated-project-migration`: explicit manual upgrade and rollback guidance for existing applications, with tested fresh-project comparison.

### Modified Capabilities

None in this planning checkpoint. During implementation, reconcile concrete deltas to `ci-pipeline`, `developer-docs`, `dotnet-template` and `metrics-observability` before changing their existing requirements. The new capabilities below add migration guarantees; they do not silently rewrite existing specifications.

## Impact

Affected areas: `Directory.Build.props`, the HTTP/stdio hosts, MCP adapter serialization/registration, HTTP protocol tests, generated-project validation, relevant CI, docs and template packaging. Application services and domain DTOs do not require redesign based on inspected code. Existing auth, hardening, observability and QA proposals remain separate; reference them rather than implementing their full scope here.

## Approval Questions

1. Approve stateless HTTP for new projects and hybrid guidance for existing session-dependent deployments?
2. Retain and test 2025-06-18 and 2025-11-25 interoperability without promising compatibility with every historical client implementation?
3. Keep feature showcases opt-in and separate from this migration?
4. If a critical existing stdio dependency-registration failure is reproduced, approve a narrowly scoped prerequisite fix with a regression test?
