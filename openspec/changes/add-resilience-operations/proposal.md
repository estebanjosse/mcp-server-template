# Change: Improve operational resilience for MCP hosts

## Why
The template lacks readiness endpoints, graceful shutdown logic, and safeguards against slow or abusive clients. Production workloads require deterministic behavior during deployments, controlled timeouts, and SSE keep-alives to avoid dropped connections.

## What Changes
- Add a readiness endpoint separate from liveness to signal when dependencies are ready.
- Implement graceful shutdown hooks, request timeouts, and back-pressure handling for long-running MCP sessions.
- Provide SSE keep-alive support and configurable limits for concurrent sessions to prevent resource exhaustion.
- Document deployment guidance covering drain procedures and timeout tuning.

## Impact
- Affected specs: resilience-operations (new)
- Affected code: src/McpServer.Template.Host.Http/Program.cs, infrastructure services, docs/
