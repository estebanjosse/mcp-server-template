## 1. Metrics Integration
- [x] 1.1 Add prometheus-net.AspNetCore package and register metrics services in the HTTP host when enabled.
- [x] 1.2 Implement configuration options binding for `Metrics:Enabled` with `MCP_METRICS_ENABLED` override and ensure the endpoint is not mapped when disabled.
- [x] 1.3 Expose the `/metrics` scraping endpoint and publish default ASP.NET metrics when enabled.

## 2. MCP Instrumentation
- [x] 2.1 Emit aggregate counters for MCP requests, tool invocations, and active sessions.
- [ ] 2.2 Emit per-tool counters alongside aggregates using prometheus-net labeled metrics.

## 3. Documentation & Tests
- [ ] 3.1 Document metrics configuration, enablement workflow, and default disabled posture.
- [ ] 3.2 Add automated tests that start the host with metrics enabled and assert the `/metrics` endpoint returns Prometheus text content.
