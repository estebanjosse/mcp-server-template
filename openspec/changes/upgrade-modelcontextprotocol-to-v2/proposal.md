## Why

The template still targets ModelContextProtocol 1.3.0 and its HTTP host assumes stateful `initialize` sessions, while the current 2.0.0 SDK and MCP 2026-07-28 specification make stateless Streamable HTTP the default deployment model. Upgrading now keeps generated servers aligned with the supported SDK and removes a session requirement that prevents ordinary stateless load balancing.

## What Changes

- Upgrade the centralized `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` package version to 2.0.0.
- Configure the HTTP MCP transport explicitly for stateless Streamable HTTP and validate request handling without a session identifier.
- Replace session-dependent HTTP transport tests with coverage for the v2 stateless protocol behavior and discovery-compatible clients.
- Update MCP request-flow telemetry so it does not represent concurrent requests as persistent MCP sessions.
- Update user and operator documentation to describe the Streamable HTTP stateless endpoint and remove legacy HTTP/SSE assumptions.
- **BREAKING**: generated HTTP servers will no longer provide stateful MCP sessions or legacy SSE compatibility by default; downstream clients must use the Streamable HTTP MCP endpoint and must not depend on session identifiers.

## Capabilities

### New Capabilities
- `stateless-mcp-http-transport`: The HTTP host serves MCP requests through an explicitly configured, sessionless Streamable HTTP transport compatible with MCP SDK v2.

### Modified Capabilities
- `metrics-observability`: MCP request-flow metrics must accurately represent stateless request concurrency rather than persistent MCP sessions.

## Impact

- Dependency management: `Directory.Build.props`, the MCP adapter, and both host projects consuming the centralized SDK version.
- HTTP runtime: `src/McpServer.Template.Host.Http/Program.cs`, its `/mcp` endpoint behavior, and HTTP integration tests.
- Observability: MCP instrumentation interfaces, Prometheus metric names and descriptions, dashboard/query compatibility, and metrics tests.
- Documentation: README and HTTP development/operations guidance for generated template consumers.
- Compatibility: existing clients using standard Streamable HTTP can migrate without session management; legacy SSE and stateful-session clients require an explicit downstream compatibility strategy.
