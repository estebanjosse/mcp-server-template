# Change: Build advanced observability foundation

## Why
Prometheus metrics cover surface-level telemetry but operators lack end-to-end visibility. Production teams need structured logging, distributed tracing hooks, and actionable alert guidance to diagnose client issues and infrastructure incidents.

## What Changes
- Adopt structured logging with correlation identifiers that flow across request processing and tool execution.
- Add tracing hooks so deployments can export spans to a chosen backend without code edits.
- Provide alerting guidance, including sample SLOs and metric-to-alert mappings.
- Document log schemas and recommended retention strategies for compliance.

## Impact
- Affected specs: observability-foundation (new)
- Affected code: src/McpServer.Template.Host.Http/Program.cs, src/McpServer.Template.Mcp/, logging configuration, docs/
