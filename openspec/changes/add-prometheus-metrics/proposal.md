# Change: Prometheus metrics for MCP HTTP host

## Why
Operators have no structured visibility into MCP server activity beyond logs and the basic health check. This blocks alerting, capacity planning, and correlating behavior with MCP usage patterns.

## What Changes
- Introduce Prometheus-compatible metrics for the HTTP host using prometheus-net.AspNetCore instrumentation.
- Expose built-in ASP.NET metrics plus MCP-specific counters for aggregate and per-tool usage when metrics are enabled.
- Guard the metrics endpoint behind a `Metrics:Enabled` configuration flag with `MCP_METRICS_ENABLED` override, defaulting to disabled.
- Document configuration and add automated coverage proving the scrape endpoint emits Prometheus text format.

## Impact
- Affected specs: metrics-observability
- Affected code: src/McpServer.Template.Host.Http/Program.cs, src/McpServer.Template.Host.Http/appsettings*.json, src/McpServer.Template.Mcp/, tests/**/*Metrics*
