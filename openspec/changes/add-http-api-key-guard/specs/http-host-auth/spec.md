## ADDED Requirements
### Requirement: MCP HTTP API Key Guard
The HTTP host MUST support an optional shared-secret guard on `/mcp` requests that activates when a non-empty value is provided via `Authentication:ApiKey` configuration or the `MCP_AUTH_API_KEY` environment variable.

#### Scenario: Guard disabled without secret
- **GIVEN** no secret is configured in `Authentication:ApiKey`
- **AND** the `MCP_AUTH_API_KEY` environment variable is unset or empty
- **WHEN** a client sends an MCP request without authentication headers
- **THEN** the server processes the request as it does today without additional authorization checks

#### Scenario: Bearer header enforced by default
- **GIVEN** `Authentication:ApiKey` is configured with `<secret>`
- **AND** no header override is configured
- **WHEN** a client sends `POST /mcp` without `Authorization: Bearer <secret>`
- **THEN** the server responds with HTTP 401 Unauthorized
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP"`

#### Scenario: Bearer header authorizes request
- **GIVEN** `Authentication:ApiKey` is configured with `<secret>`
- **WHEN** a client sends `Authorization: Bearer <secret>` with its MCP request
- **THEN** the guard allows the request to reach the MCP pipeline without modification

#### Scenario: Custom header override
- **GIVEN** `Authentication:ApiKey` is configured with `<secret>`
- **AND** `Authentication:HeaderName` (or `MCP_AUTH_HEADER`) is set to `X-MCP-Key`
- **WHEN** a client sends `X-MCP-Key: <secret>` with its MCP request
- **THEN** the guard treats the request as authenticated without requiring an `Authorization` header

#### Scenario: Guard blocks invalid credentials
- **GIVEN** `Authentication:ApiKey` is configured with `<secret>`
- **WHEN** a client presents any other credential value
- **THEN** the server responds with HTTP 401 Unauthorized
- **AND** the response includes `WWW-Authenticate: Bearer realm="MCP"`

#### Scenario: Monitoring endpoints remain open
- **GIVEN** the guard is enabled by configuration
- **WHEN** a client calls `/health` or the root `/` endpoint without authentication headers
- **THEN** the server responds without invoking the API key guard
