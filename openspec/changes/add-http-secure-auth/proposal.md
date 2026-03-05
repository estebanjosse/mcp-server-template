# Change: JWT multi-tenant authentication for MCP HTTP host (secure mode)

## Why
The `simple` API key mode covers single-user and machine-to-machine deployments, but internet-facing production scenarios with multiple clients or tenants require stronger guarantees: cryptographic token validation, per-tenant isolation, granular scope-based permissions, and differentiated rate limits. The `secure` mode fills this gap using industry-standard JWT Bearer authentication with JWKS discovery.

## What Changes
- Implement the `secure` authentication strategy that replaces the 501 placeholder introduced by `add-http-api-key-guard`.
- Validate JWT Bearer tokens against a configurable JWKS authority (`Authentication:Secure:Authority`), with audience and issuer checks.
- Extract multi-tenant identity from configurable JWT claims and build a `TenantContext` accessible to downstream middleware and MCP handlers.
- Enforce scope-based permissions (`mcp:read`, `mcp:execute`, `mcp:admin`) mapped to MCP operations.
- Introduce trust tiers per tenant (`basic`, `standard`, `premium`) configured locally, determining rate limits and optionally narrowing allowed scopes.
- Emit per-tenant audit log entries for all authentication and authorization decisions.
- Return `401 Unauthorized` for invalid/expired tokens, `403 Forbidden` for insufficient scopes or tier restrictions.

## What Does NOT Change
- The mode-switching foundation (`Authentication:Mode`), strategy pattern, and `/mcp` scoping remain as delivered by `add-http-api-key-guard`.
- The `none` and `simple` modes are unaffected.
- The security hardening layer (TLS, rate limiting, headers, CORS) remains in `add-http-security-hardening`.

## Impact
- Affected specs: http-secure-auth (new)
- Affected code: src/McpServer.Template.Host.Http/ (new strategy, options, tenant context), appsettings*.json
- Dependencies: requires `add-http-api-key-guard` (mode switching foundation) to be implemented first
