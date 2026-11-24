# MCP 2026-07-28 compatibility

The unreleased template candidate targets .NET 10 and C# SDK 2.4.0. The [migration guide](mcp-2026-07-28.md) is for existing applications; the [advanced notes](mcp-2026-07-28-advanced.md) cover session-dependent deployments and custom protocol features.

## Locally qualified paths

| Client or fixture | Transport | Revision | Verified scope |
| --- | --- | --- | --- |
| Official C# SDK 2.4.0 | Direct HTTP and through NGINX 1.26.3 on loopback | `2026-07-28`, `2025-11-25`, `2025-06-18` | Modern discovery or historical initialize, sample tool calls, prompt and resource reads; no persistent session or silent downgrade. |
| MCP Inspector CLI 2.13.0 | Direct HTTP and through the same NGINX | `2026-07-28` and `2025-11-25` | Exact sample divide result in modern and legacy modes. The legacy client's optional GET received `405` without blocking the tool call. |
| Template integration and generated-host probes | HTTP and stdio | `2026-07-28`, `2025-11-25`, `2025-06-18` | Tools where advertised, prompts and resources; stdio protocol-only stdout and clean EOF. Packed-template matrix covers all 12 host/test/sample combinations, including live hosts with tests excluded. |
| Customized project generated from template 1.4.0 | HTTP and stdio | Three revisions after upgrade; both historical revisions before and after rollback | Preserved the custom tool, service and host configuration. Local rollback restored working baseline source, SDK and HTTP behavior. See the [HTTP fixture update](mcp-2026-07-28.md#4-build-and-check-your-application) before rerunning older generated tests. |

Interoperability and migration evidence is from candidate source `72a39206f14bfe00e405e4c89cfb253665ba7c79`. The HTTP proxy run used API-key authentication and exercised header forwarding and rejection, explicit cancellation and a proxy read timeout **before** response headers. Server-side cancellation and handler exit were confirmed in a disposable fixture, followed by a successful call on the same client.

## Boundaries

- The default HTTP endpoint is stateless Streamable HTTP on `/mcp`. Older initialize-based clients must work without session IDs, separate session GET/DELETE operations, or a legacy `/sse` endpoint. SSE in a POST response is still supported.
- Inspector stdio and browser UI, other external clients and proxies, TLS/HTTP2, production topology, and timeout after SSE headers were sent were **not run** in the external qualification. The stdio fixture is not a substitute for a named external client. A local rollback is not a deployment rollback.
- This is not a support promise for untested clients or a substitute for exact release-head CI. Test your own client versions, proxy, authentication, deadlines and cancellation before deploying.
