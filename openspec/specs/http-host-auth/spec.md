# http-host-auth Specification

## Purpose
TBD - created by archiving change add-http-api-key-guard. Update Purpose after archive.

## Requirements
### Requirement: Authentication Mode Selection
The HTTP host SHALL support three authentication modes switchable via configuration, enabling progressive security postures without code changes.

#### Scenario: Default mode is none
- **GIVEN** `Authentication:Mode` is not configured
- **AND** the `MCP_AUTH_MODE` environment variable is unset
- **WHEN** the host starts
- **THEN** the authentication mode is `none`
- **AND** all requests to `/mcp` are processed without authorization checks

#### Scenario: Mode set via configuration file
- **GIVEN** `Authentication:Mode` is set to `simple` in appsettings
- **WHEN** the host starts
- **THEN** the simple (API key) authentication strategy is activated for `/mcp`

#### Scenario: Environment variable overrides configuration file
- **GIVEN** `Authentication:Mode` is set to `none` in appsettings
- **AND** `MCP_AUTH_MODE` is set to `simple`
- **WHEN** the host starts
- **THEN** the active mode is `simple` (environment variable wins)

#### Scenario: Invalid mode rejected at startup
- **GIVEN** `Authentication:Mode` is set to `fancy`
- **WHEN** the host starts
- **THEN** startup fails with a descriptive error listing valid modes (`none`, `simple`, `secure`)

#### Scenario: Secure mode returns 501 until implemented
- **GIVEN** `Authentication:Mode` is set to `secure`
- **AND** the secure authentication strategy is not yet registered
- **WHEN** a client sends a request to `/mcp`
- **THEN** the server responds with HTTP 501 Not Implemented
- **AND** the response body indicates that the `secure` mode requires the secure auth package

### Requirement: Strategy-Based Authentication Pipeline
The authentication middleware SHALL dispatch to the active mode's strategy, keeping the pipeline path-scoped to `/mcp` only.

#### Scenario: Monitoring endpoints bypass authentication
- **GIVEN** any authentication mode is active (including `simple` or `secure`)
- **WHEN** a client calls `/health` or the root `/` endpoint without credentials
- **THEN** the server responds normally without invoking any authentication logic

#### Scenario: Only /mcp is guarded
- **GIVEN** mode is `simple` with a configured API key
- **WHEN** a client calls `/health` without an `Authorization` header
- **THEN** the request succeeds with HTTP 200
- **AND** when the same client calls `/mcp` without the header
- **THEN** the server responds with HTTP 401 Unauthorized

### Requirement: Simple Mode - API Key Guard
When mode is `simple`, the HTTP host SHALL enforce a shared-secret guard on `/mcp` with constant-time comparison, configurable header, and dual-key rotation support.

#### Scenario: Bearer header enforced by default
- **GIVEN** mode is `simple`
- **AND** `Authentication:ApiKeys` contains `["<secret>"]`
- **AND** no header override is configured
- **WHEN** a client sends `POST /mcp` without `Authorization: Bearer <secret>`
- **THEN** the server responds with HTTP 401 Unauthorized
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP"`

#### Scenario: Bearer header authorizes request
- **GIVEN** mode is `simple` with `Authentication:ApiKeys` containing `["<secret>"]`
- **WHEN** a client sends `Authorization: Bearer <secret>` with its MCP request
- **THEN** the guard allows the request to reach the MCP pipeline without modification

#### Scenario: Custom header override
- **GIVEN** mode is `simple`
- **AND** `Authentication:HeaderName` (or `MCP_AUTH_HEADER`) is set to `X-MCP-Key`
- **AND** `Authentication:ApiKeys` contains `["<secret>"]`
- **WHEN** a client sends `X-MCP-Key: <secret>` with its MCP request
- **THEN** the guard treats the request as authenticated without requiring an `Authorization` header

#### Scenario: Constant-time comparison prevents timing attacks
- **GIVEN** mode is `simple` with a configured API key
- **WHEN** the guard compares the presented credential to the stored key
- **THEN** comparison uses `CryptographicOperations.FixedTimeEquals` (or equivalent)
- **AND** response time does not vary measurably between near-match and complete-mismatch inputs

#### Scenario: Minimum key length enforced at startup
- **GIVEN** mode is `simple`
- **AND** `Authentication:ApiKeys` contains a key shorter than 32 characters
- **WHEN** the host starts
- **THEN** startup fails with a descriptive error indicating the minimum required key length (32 characters)

#### Scenario: Single string value coerced to array
- **GIVEN** `Authentication:ApiKeys` is set to a single string `"my-secret-that-is-long-enough-32c"` (not an array)
- **WHEN** the host starts
- **THEN** the value is coerced to `["my-secret-that-is-long-enough-32c"]` and the guard operates normally

### Requirement: Dual-Key Rotation in Simple Mode
The simple mode SHALL accept multiple API keys simultaneously to enable zero-downtime key rotation.

#### Scenario: Both old and new keys accepted during rotation
- **GIVEN** mode is `simple`
- **AND** `Authentication:ApiKeys` contains `["<old-key>", "<new-key>"]`
- **WHEN** a client presents `Authorization: Bearer <old-key>`
- **THEN** the request is authenticated
- **AND** when another client presents `Authorization: Bearer <new-key>`
- **THEN** the request is also authenticated

#### Scenario: Each key validated with constant-time comparison
- **GIVEN** `Authentication:ApiKeys` contains multiple keys
- **WHEN** the guard evaluates a credential
- **THEN** every configured key is compared using constant-time comparison
- **AND** the guard returns authenticated if any key matches (without short-circuiting to avoid timing leaks)

#### Scenario: Empty keys filtered on startup
- **GIVEN** `Authentication:ApiKeys` contains `["<valid-key>", "", " "]`
- **WHEN** the host starts
- **THEN** empty and whitespace-only entries are silently discarded
- **AND** validation continues with the remaining valid keys

### Requirement: Authentication Event Logging
The authentication middleware SHALL emit structured log events for security-relevant authentication activity without leaking secrets.

#### Scenario: Successful authentication logged
- **GIVEN** mode is `simple` or `secure`
- **WHEN** a request is successfully authenticated
- **THEN** a structured log entry is emitted at Information level containing: timestamp, client IP, endpoint path, auth mode, and result (`success`)
- **AND** the log entry does NOT contain the credential value or any key material

#### Scenario: Failed authentication logged
- **GIVEN** mode is `simple` or `secure`
- **WHEN** a request fails authentication
- **THEN** a structured log entry is emitted at Warning level containing: timestamp, client IP, endpoint path, auth mode, result (`denied`), and failure reason (`missing_credential` | `invalid_credential`)
- **AND** the log entry does NOT contain the presented credential

#### Scenario: Secrets never appear in logs
- **GIVEN** any authentication mode
- **WHEN** any log entry related to authentication is emitted (including at Debug or Trace level)
- **THEN** the entry MUST NOT contain API key values, JWT tokens, or any credential material
- **AND** key references use a masked format (e.g., `key:***<last4>`)

### Requirement: Brute-Force Mitigation
The simple mode SHALL include basic brute-force protection to slow down credential guessing attacks.

#### Scenario: Incremental delay after consecutive failures
- **GIVEN** mode is `simple`
- **WHEN** a client IP produces more than 5 failed authentication attempts within a 60-second window
- **THEN** subsequent 401 responses from that IP include a `Retry-After` header
- **AND** the delay increases progressively (e.g., 1s, 2s, 4s, 8s, capped at 30s)

#### Scenario: Successful auth resets failure counter
- **GIVEN** a client IP has accumulated failed attempts
- **WHEN** the client successfully authenticates
- **THEN** the failure counter for that IP is reset to zero

---