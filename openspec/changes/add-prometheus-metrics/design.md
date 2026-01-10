## Context
The template exposes MCP functionality through HTTP and stdio hosts but only provides a JSON `/health` endpoint for observability. Operators have requested Prometheus-compatible metrics to plug into existing monitoring stacks. The change must add instrumentation without impacting deployments that do not opt in.

## Goals / Non-Goals
- Goals: Enable Prometheus scraping for the HTTP host, surface ASP.NET request metrics, publish MCP-specific counters (aggregate and per-tool), respect configuration toggle.
- Non-Goals: Add metrics to the stdio host, implement authentication for metrics, introduce long-term storage or alerting.

## Decisions
- Decision: Use prometheus-net.AspNetCore middleware for instrumentation because it is lightweight, widely adopted in .NET, and directly exposes Prometheus text endpoints without additional dependencies.
- Decision: Gate metrics behind `Metrics:Enabled` config (default false) with `MCP_METRICS_ENABLED` override to prevent accidental exposure and align with existing configuration patterns.
- Decision: Register custom collectors that track aggregate counters (requests handled, tool invocations, session counts) and expose per-tool labels using prometheus-net counters to support both fleet-wide and tool-specific dashboards.
- Decision: Map `/metrics` only when enabled to avoid leaking an unauthenticated endpoint. When disabled the middleware is not registered.

## Risks / Trade-offs
- Risk: Additional middleware when enabled could increase request overhead. Mitigation: prometheus-net collects asynchronously and we will document the optional nature plus monitor during testing.
- Risk: Prometheus endpoint is unauthenticated. Mitigation: keep disabled by default and recommend network-layer protections in documentation.

## Open Questions
- None at this time.
