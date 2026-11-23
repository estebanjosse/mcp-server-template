# Release notes: MCP 2026-07-28 template update

**Unreleased.** The template package version and publication date are not assigned here. The C# SDK version is not the template package version.

## Breaking changes

- **HTTP is stateless.** `/mcp` no longer issues session IDs or accepts session GET/DELETE operations. Independent legacy SSE endpoints are not provided. Streamable HTTP can still return SSE from a POST. Older `2025-11-25` and `2025-06-18` initialize-based clients can continue ordinary calls if they do not depend on sessions.
- **Modern clients use a different bootstrap and wire contract.** For `2026-07-28`, send per-request metadata instead of `initialize`. Clients can call `server/discover` to select a revision up front but do not need to call it before a conforming request. Check custom result, schema, cache and error parsing against the negotiated revision.
- **Metric rename:** `mcp_requests_in_flight` replaces `mcp_sessions_active` without an alias. It measures concurrent MCP HTTP requests admitted past authentication, not sessions. Update alert and dashboard queries and reassess thresholds. Request and tool counters remain available.

## Other changes

- .NET 10 scaffolds use aligned MCP C# SDK packages at 2.4.0. Stdio logs go to stderr, leaving stdout for protocol frames.
- Select hosts with `--host http|stdio|both` (or the existing `--http-host` and `--stdio-host` flags). No-host selections are rejected. Tests and sample tools remain opt-in.
- The content-only template package includes the third-party notice; new scaffolds retain it. The template package does not bundle the SDK binaries. It does not generate the repository's GitHub workflows or validation projects.

Updating the installed template does not change existing applications. To adopt the new SDK in an existing project, follow the [optional migration guide](mcp-2026-07-28.md) and review the [compatibility matrix](mcp-2026-07-28-compatibility.md). This update does not add new authentication modes or showcases for Tasks, Apps, Skills or MRTR.
