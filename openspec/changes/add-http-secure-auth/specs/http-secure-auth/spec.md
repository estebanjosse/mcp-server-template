## ADDED Requirements

### Requirement: JWT Bearer Token Validation
When mode is `secure`, the HTTP host SHALL validate JWT Bearer tokens against a configurable JWKS authority, rejecting requests with invalid, expired, or tampered tokens.

#### Scenario: Valid JWT accepted
- **GIVEN** mode is `secure`
- **AND** `Authentication:Secure:Authority` is configured to `https://idp.example.com`
- **AND** `Authentication:Secure:Audience` is configured to `mcp-server`
- **WHEN** a client sends `Authorization: Bearer <valid-jwt>` signed by the authority's key
- **AND** the token's `aud` claim matches `mcp-server`
- **AND** the token is not expired
- **THEN** the request is authenticated and forwarded to the MCP pipeline

#### Scenario: Expired JWT rejected
- **GIVEN** mode is `secure` with a valid authority configured
- **WHEN** a client sends a JWT whose `exp` claim is in the past
- **THEN** the server responds with HTTP 401 Unauthorized
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP", error="invalid_token", error_description="Token has expired"`

#### Scenario: Invalid signature rejected
- **GIVEN** mode is `secure`
- **WHEN** a client sends a JWT signed by an unknown key not present in the JWKS endpoint
- **THEN** the server responds with HTTP 401 Unauthorized

#### Scenario: Missing or malformed token rejected
- **GIVEN** mode is `secure`
- **WHEN** a client sends a request without an `Authorization` header, or with a non-Bearer scheme, or with a malformed token
- **THEN** the server responds with HTTP 401 Unauthorized
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP"`

#### Scenario: Audience mismatch rejected
- **GIVEN** `Authentication:Secure:Audience` is `mcp-server`
- **WHEN** a client sends a valid JWT whose `aud` claim does not include `mcp-server`
- **THEN** the server responds with HTTP 401 Unauthorized

#### Scenario: Issuer validated
- **GIVEN** `Authentication:Secure:Issuer` is configured (or derived from Authority)
- **WHEN** a client sends a JWT with a different `iss` claim
- **THEN** the server responds with HTTP 401 Unauthorized

### Requirement: JWKS Discovery and Key Caching
The secure strategy SHALL discover signing keys via the authority's JWKS endpoint with appropriate caching.

#### Scenario: JWKS keys fetched from authority
- **GIVEN** `Authentication:Secure:Authority` is `https://idp.example.com`
- **WHEN** the server needs to validate a JWT signature
- **THEN** it fetches public keys from `https://idp.example.com/.well-known/openid-configuration` (or direct JWKS URI)
- **AND** caches the keys according to the JWKS response's cache headers

#### Scenario: Key rotation handled gracefully
- **GIVEN** the authority rotates its signing keys
- **WHEN** a JWT signed with a new key arrives and the cached keyset does not contain the `kid`
- **THEN** the server refreshes the JWKS keyset once
- **AND** re-validates the token with the updated keys

### Requirement: Secure Mode Configuration
The secure mode SHALL be fully configurable via `Authentication:Secure:*` settings with environment variable overrides.

#### Scenario: Configuration via appsettings
- **GIVEN** the following configuration:
  ```json
  {
    "Authentication": {
      "Mode": "secure",
      "Secure": {
        "Authority": "https://idp.example.com",
        "Audience": "mcp-server",
        "Issuer": "https://idp.example.com",
        "TenantClaim": "tenant_id",
        "ClientIdClaim": "client_id",
        "TierClaim": "tier",
        "DefaultTier": "basic",
        "RejectUnknownTenants": false
      }
    }
  }
  ```
- **WHEN** the host starts
- **THEN** JWT validation uses these values for authority, audience, and issuer checks
- **AND** tenant and client identity are extracted from the specified claims
- **AND** tier resolution uses the configured claim name, default tier, and rejection policy

#### Scenario: Environment variable overrides
- **GIVEN** `MCP_AUTH_SECURE_AUTHORITY` is set
- **WHEN** the host starts
- **THEN** the environment variable value overrides `Authentication:Secure:Authority` from appsettings

#### Scenario: Missing authority rejected at startup
- **GIVEN** mode is `secure`
- **AND** `Authentication:Secure:Authority` is not configured
- **WHEN** the host starts
- **THEN** startup fails with a descriptive error indicating the required configuration key

### Requirement: Multi-Tenant Identity Extraction
The secure strategy SHALL extract tenant identity from JWT claims and build a `TenantContext` accessible to downstream components.

#### Scenario: Tenant context populated from JWT
- **GIVEN** mode is `secure`
- **AND** `Authentication:Secure:TenantClaim` is `tenant_id` (default)
- **WHEN** a valid JWT contains `"tenant_id": "acme"`
- **THEN** a `TenantContext` is created with `TenantId = "acme"`
- **AND** the context is accessible via DI to downstream middleware and MCP handlers

#### Scenario: Client identity extracted
- **GIVEN** `Authentication:Secure:ClientIdClaim` is `client_id` (default)
- **WHEN** a valid JWT contains `"client_id": "client-42"`
- **THEN** `TenantContext.ClientId` is set to `client-42`

#### Scenario: Missing tenant claim rejected
- **GIVEN** mode is `secure`
- **WHEN** a valid JWT does not contain the configured tenant claim
- **THEN** the server responds with HTTP 403 Forbidden
- **AND** the audit log records the reason (`missing_tenant_claim`)

### Requirement: Scope-Based Permission Enforcement
The secure strategy SHALL enforce granular permissions based on JWT scopes mapped to MCP operations.

#### Scenario: mcp:read allows listing operations
- **GIVEN** a valid JWT with `scope` containing `mcp:read`
- **WHEN** the client lists tools, prompts, or resources
- **THEN** the operations succeed

#### Scenario: mcp:execute allows tool invocation
- **GIVEN** a valid JWT with `scope` containing `mcp:execute`
- **WHEN** the client invokes a tool
- **THEN** the invocation succeeds
- **AND** the client can also perform read operations (execute implies read)

#### Scenario: mcp:admin grants full access
- **GIVEN** a valid JWT with `scope` containing `mcp:admin`
- **WHEN** the client performs any MCP operation
- **THEN** all operations succeed without scope restrictions

#### Scenario: Insufficient scope denied
- **GIVEN** a valid JWT with only `mcp:read` scope
- **WHEN** the client attempts to invoke a tool
- **THEN** the server responds with HTTP 403 Forbidden
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP", error="insufficient_scope", error_description="Required scope: mcp:execute"`

#### Scenario: No MCP scopes present
- **GIVEN** a valid JWT with no `mcp:*` scopes
- **WHEN** the client sends any request to `/mcp`
- **THEN** the server responds with HTTP 403 Forbidden

### Requirement: Trust Tiers
The secure mode SHALL support trust tiers that determine rate limits and optionally restrict scopes. Tiers are defined globally and resolved per-tenant via a cascading priority: local override → JWT claim → default tier.

#### Scenario: Tier definitions
- **GIVEN** the configuration defines trust tiers:
  ```json
  {
    "Authentication": {
      "Secure": {
        "Tiers": {
          "basic":    { "RequestsPerMinute": 10,  "MaxScope": "mcp:read"    },
          "standard": { "RequestsPerMinute": 60,  "MaxScope": "mcp:execute" },
          "premium":  { "RequestsPerMinute": 300, "MaxScope": "mcp:admin"   }
        }
      }
    }
  }
  ```
- **WHEN** a tenant is assigned a tier
- **THEN** the corresponding rate limit and maximum scope are enforced

#### Scenario: Trust tiers define rate limits
- **GIVEN** a tenant resolved to tier `basic` with `RequestsPerMinute: 10`
- **WHEN** the tenant exceeds 10 requests in a minute
- **THEN** subsequent requests receive HTTP 429 Too Many Requests with a `Retry-After` header

#### Scenario: Trust tier restricts maximum scope
- **GIVEN** tier `basic` has `MaxScope` set to `mcp:read`
- **AND** a JWT for a `basic` tenant contains `mcp:execute` scope
- **WHEN** the client attempts to invoke a tool
- **THEN** the server responds with HTTP 403 Forbidden
- **AND** the effective scope is capped to `mcp:read` regardless of JWT claims

### Requirement: Tier Resolution — Cascading Priority
The tier for a tenant SHALL be resolved using a cascading priority: local configuration override wins over JWT claim, which wins over the default tier.

#### Scenario: Priority 1 — Local override wins
- **GIVEN** the local `Tenants` map contains `"acme": { "Tier": "basic" }`
- **AND** the JWT for tenant `acme` contains `"tier": "premium"`
- **WHEN** the tier is resolved
- **THEN** the effective tier is `basic` (local override takes precedence)
- **AND** the audit log records `tier_source: "config_override"`

#### Scenario: Priority 2 — JWT claim used when no local override
- **GIVEN** tenant `beta-corp` is NOT present in the local `Tenants` map
- **AND** the JWT contains `"tier": "standard"`
- **AND** `Authentication:Secure:TierClaim` is `tier` (default)
- **WHEN** the tier is resolved
- **THEN** the effective tier is `standard` (from JWT claim)
- **AND** the audit log records `tier_source: "jwt_claim"`

#### Scenario: Priority 3 — Default tier as fallback
- **GIVEN** tenant `unknown-corp` is NOT in the local `Tenants` map
- **AND** the JWT does NOT contain a `tier` claim
- **AND** `Authentication:Secure:DefaultTier` is `basic` (default)
- **WHEN** the tier is resolved
- **THEN** the effective tier is `basic`
- **AND** the audit log records `tier_source: "default"`

#### Scenario: Invalid tier name in JWT falls back to default
- **GIVEN** a JWT contains `"tier": "ultra-vip"`
- **AND** `ultra-vip` is not defined in the `Tiers` configuration
- **WHEN** the tier is resolved
- **THEN** the effective tier falls back to `DefaultTier`
- **AND** a Warning log is emitted indicating the unknown tier value was ignored

#### Scenario: Local override for emergency downgrade
- **GIVEN** tenant `compromised-corp` was previously tier `premium` via JWT
- **AND** an operator adds `"compromised-corp": { "Tier": "basic" }` to the local `Tenants` map
- **WHEN** the next request from `compromised-corp` arrives
- **THEN** the effective tier is `basic` regardless of the JWT tier claim
- **AND** the effective scopes are capped to `mcp:read`

### Requirement: Unknown Tenant Rejection
The secure mode SHALL optionally reject tenants not explicitly mapped in local configuration for strict-access deployments.

#### Scenario: Unknown tenants allowed by default
- **GIVEN** `Authentication:Secure:RejectUnknownTenants` is not configured (defaults to `false`)
- **WHEN** a JWT for an unmapped tenant is validated
- **THEN** the tier is resolved via JWT claim or default tier
- **AND** the request proceeds normally

#### Scenario: Unknown tenants rejected in strict mode
- **GIVEN** `Authentication:Secure:RejectUnknownTenants` is set to `true`
- **AND** the JWT contains `"tenant_id": "surprise-corp"`
- **AND** `surprise-corp` is NOT in the local `Tenants` map
- **WHEN** the request is processed
- **THEN** the server responds with HTTP 403 Forbidden
- **AND** the audit log records the reason (`unknown_tenant_rejected`)

#### Scenario: Strict mode still respects JWT-claimed tier for mapped tenants
- **GIVEN** `RejectUnknownTenants` is `true`
- **AND** `"acme"` is in the local `Tenants` map with no tier override (entry exists but `Tier` is null)
- **AND** the JWT contains `"tier": "premium"`
- **WHEN** the tier is resolved
- **THEN** the effective tier is `premium` (from JWT claim, local entry acts as allowlist without overriding tier)

### Requirement: Tier Configuration Defaults
The tier system SHALL have sensible defaults that minimize required configuration.

#### Scenario: TierClaim defaults to "tier"
- **GIVEN** `Authentication:Secure:TierClaim` is not configured
- **WHEN** a JWT contains a `tier` claim
- **THEN** the claim is used with the default claim name `tier`

#### Scenario: DefaultTier defaults to "basic"
- **GIVEN** `Authentication:Secure:DefaultTier` is not configured
- **WHEN** a tier cannot be resolved from local config or JWT
- **THEN** the fallback tier is `basic`

#### Scenario: Tiers section optional for simple deployments
- **GIVEN** the `Tiers` section is not configured
- **WHEN** the host starts in `secure` mode
- **THEN** a single implicit `basic` tier is used with conservative defaults (`RequestsPerMinute: 30`, `MaxScope: "mcp:execute"`)
- **AND** a Warning log indicates that no tiers are configured and defaults are in effect

### Requirement: Tenant Isolation
The secure mode SHALL enforce strict isolation between tenants.

#### Scenario: Tenant A cannot access Tenant B's sessions
- **GIVEN** two concurrent clients authenticated as tenant `acme` and tenant `beta`
- **WHEN** tenant `acme` attempts to interact with a session established by tenant `beta`
- **THEN** the server responds with HTTP 403 Forbidden

#### Scenario: Per-tenant metrics separation
- **GIVEN** the metrics system is enabled
- **WHEN** requests from different tenants are processed
- **THEN** metrics are labeled with `tenant` allowing per-tenant observability

### Requirement: Secure Mode Audit Logging
The secure strategy SHALL emit detailed structured audit logs for all authentication and authorization decisions.

#### Scenario: Authenticated request audit trail
- **GIVEN** mode is `secure`
- **WHEN** a request is successfully authenticated and authorized
- **THEN** a structured log entry at Information level contains: timestamp, tenant_id, client_id, client IP, scopes granted, trust tier, and result (`success`)

#### Scenario: Authorization denial audit trail
- **GIVEN** mode is `secure`
- **WHEN** a request is authenticated but denied due to insufficient scope or tier restriction
- **THEN** a structured log entry at Warning level contains: timestamp, tenant_id, client_id, client IP, denied operation, required scope, actual scopes, and trust tier

#### Scenario: Token values never logged
- **GIVEN** mode is `secure`
- **WHEN** any audit log entry is emitted
- **THEN** the entry MUST NOT contain the raw JWT token
- **AND** client references use `client_id` from claims, never the token itself
