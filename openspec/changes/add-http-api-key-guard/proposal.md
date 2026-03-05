# Change: Authentication mode switching and API Key Guard for MCP HTTP host

## Why
The HTTP host currently exposes the `/mcp` endpoint without authentication, which blocks deployments that need even basic access control. Different deployment scenarios require different security postures — from wide-open local development to shared-secret machine-to-machine communication. A mode-switching foundation enables progressive security without forcing complexity on simple use cases.

## What Changes
- Introduce `Authentication:Mode` configuration (`none` | `simple` | `secure`) with `MCP_AUTH_MODE` environment variable override, defaulting to `none`.
- Implement a strategy-based authentication pipeline scoped to `/mcp` that dispatches to the active mode's handler.
- Deliver the `simple` mode: shared-secret API key guard with constant-time comparison, configurable header, dual-key rotation support, minimum key length enforcement, and structured auth event logging.
- Register the `secure` mode as a slot that returns 501 Not Implemented until `add-http-secure-auth` is delivered.
- Ensure `/health` and monitoring endpoints remain publicly accessible regardless of mode.
- Return `WWW-Authenticate: Bearer realm="MCP"` on unauthorized responses to aid client diagnostics.

## Impact
- Affected specs: http-host-auth (rewritten)
- Affected code: src/McpServer.Template.Host.Http/Program.cs, appsettings*.json, new Options/AuthenticationOptions.cs, new middleware
- **BREAKING**: `Authentication:ApiKey` replaced by `Authentication:ApiKeys` (string array). A single string value is still accepted and coerced to a single-element array for backward compatibility.
