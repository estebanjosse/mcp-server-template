# Change: Optional API Key Guard for MCP HTTP host

## Why
The HTTP host currently exposes the `/mcp` endpoint without authentication, which blocks deployments that need a lightweight shared-secret guard while keeping health checks open for monitoring.

## What Changes
- Introduce an optional API key middleware that activates when a non-empty secret is configured.
- Provide configuration via `Authentication:ApiKey` and `Authentication:HeaderName`, with environment variable overrides.
- Return `WWW-Authenticate: Bearer realm="MCP"` on unauthorized responses to aid client diagnostics.
- Ensure `/health` and other monitoring endpoints remain publicly accessible.

## Impact
- Affected specs: http-host-auth
- Affected code: src/McpServer.Template.Host.Http/Program.cs, src/McpServer.Template.Host.Http/appsettings*.json
