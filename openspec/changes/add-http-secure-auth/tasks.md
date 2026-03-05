## 1. Foundation — JWT validation and secure strategy
- [ ] 1.1 Add `SecureAuthenticationOptions` with `Authority`, `Audience`, `Issuer`, `TenantClaim`, `ClientIdClaim`, and corresponding `MCP_AUTH_SECURE_*` env var overrides.
- [ ] 1.2 Add startup validation: reject missing Authority when mode is `secure`, validate URI format.
- [ ] 1.3 Implement `JwtAuthStrategy` (replaces 501 placeholder): validate JWT Bearer via JWKS discovery, check signature, expiry, audience, issuer.
- [ ] 1.4 Configure JWKS key caching with automatic refresh on unknown `kid`.
- [ ] 1.5 Add tests: valid JWT accepted, expired JWT rejected, invalid signature rejected, missing token 401, audience mismatch, issuer mismatch, missing authority fails startup.

## 2. Multi-tenant identity and context
- [ ] 2.1 Create `TenantContext` (TenantId, ClientId, Tier, Scopes) and register as scoped service populated by the secure strategy.
- [ ] 2.2 Extract tenant and client identity from configurable JWT claims (defaults: `tenant_id`, `client_id`).
- [ ] 2.3 Reject JWTs with missing tenant claim with 403.
- [ ] 2.4 Add tests: tenant context populated correctly, missing tenant claim returns 403, custom claim names honored.

## 3. Scope-based permissions
- [ ] 3.1 Define MCP scope model: `mcp:read` (list tools/prompts/resources), `mcp:execute` (invoke tools, implies read), `mcp:admin` (all operations).
- [ ] 3.2 Implement scope enforcement middleware/filter that checks required scope per MCP operation type.
- [ ] 3.3 Return 403 with `WWW-Authenticate` error description for insufficient scope.
- [ ] 3.4 Add tests: read-only scope blocks execution, execute allows read, admin grants all, no MCP scopes returns 403, scope error description in response.

## 4. Trust tiers and per-tenant rate limiting
- [ ] 4.1 Add `Tiers` configuration section with per-tier `RequestsPerMinute` and optional `MaxScope`. Add `TierClaim`, `DefaultTier`, and `RejectUnknownTenants` to `SecureAuthenticationOptions`.
- [ ] 4.2 Implement tier resolution cascade: local override (`Tenants` map) → JWT `TierClaim` → `DefaultTier`. Log `tier_source` in audit.
- [ ] 4.3 Implement per-tenant rate limiting based on resolved tier, return 429 with `Retry-After`.
- [ ] 4.4 Enforce `MaxScope` ceiling: cap effective scopes to tier maximum regardless of JWT claims.
- [ ] 4.5 Implement `RejectUnknownTenants`: when true, reject tenants not in local `Tenants` map with 403.
- [ ] 4.6 Add tests: cascade priority (local > JWT > default), invalid tier name in JWT falls back, emergency downgrade via local override, unknown tenant rejection, default tier for unmapped tenant, MaxScope ceiling, rate limit per tier, Tiers section optional with implicit defaults.

## 5. Tenant isolation and audit
- [ ] 5.1 Enforce tenant isolation: reject cross-tenant session access with 403.
- [ ] 5.2 Emit per-tenant structured audit logs (timestamp, tenant_id, client_id, IP, scopes, tier, result) — never log token values.
- [ ] 5.3 Add `tenant` label to metrics when available for per-tenant observability.
- [ ] 5.4 Add tests: cross-tenant access denied, audit log fields present, no raw JWT in logs, metrics labeled by tenant.

## 6. Configuration and documentation
- [ ] 6.1 Update `appsettings.json` with `Authentication:Secure` section (Authority, Audience, Issuer, TenantClaim, ClientIdClaim, TierClaim, DefaultTier, RejectUnknownTenants, Tiers, Tenants) with commented examples.
- [ ] 6.2 Document secure mode setup: IdP configuration, scope creation, tier resolution cascade, tenant override workflow, emergency downgrade procedure, and operational checklist in docs/.
