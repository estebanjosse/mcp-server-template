## REMOVED Requirements

### Requirement: MCP Aggregate Metrics
**Reason**: The active-session scenario describes persistent transport sessions, but the implementation measures HTTP request concurrency and the proposed default is stateless.
**Migration**: Replace this requirement with MCP Request-Flow Metrics below. Retain aggregate request/tool counters, remove `mcp_sessions_active`, and migrate dashboards/alerts to `mcp_requests_in_flight`; do not preserve a misleading session alias.

## ADDED Requirements

### Requirement: MCP Request-Flow Metrics
When enabled, the system MUST preserve aggregate request/tool counters and measure admitted HTTP `/mcp` request concurrency as `mcp_requests_in_flight`, after authentication and before SDK dispatch. Each admitted request MUST increment the counter once and release its gauge increment exactly once on exit. The old session gauge MUST be removed with dashboard migration guidance. Metrics MUST remain disabled by default.

#### Scenario: Requests counter increments
- **GIVEN** metrics are enabled
- **WHEN** an authenticated or authentication-exempt MCP HTTP request passes the existing guard
- **THEN** `mcp_requests_total` increases by one
- **AND** discovery and requests later rejected by the SDK follow the same counting rule.

#### Scenario: Tool invocations counter increments
- **GIVEN** metrics are enabled
- **WHEN** any MCP tool invocation completes
- **THEN** a counter named `mcp_tool_invocations_total` increases by one.

#### Scenario: In-flight gauge reflects concurrent work
- **GIVEN** metrics are enabled and two admitted MCP HTTP requests are held in progress
- **WHEN** the gauge is scraped
- **THEN** `mcp_requests_in_flight` is two above its initial value
- **AND** completing each request reduces that value by one and returns it to its initial value after both finish.

#### Scenario: Failure and cancellation release the gauge
- **GIVEN** metrics are enabled and an admitted request is in flight
- **WHEN** that request fails, is canceled or its response stream closes
- **THEN** its gauge increment is released exactly once in bounded cleanup
- **AND** no residual increment or negative gauge value remains.

#### Scenario: Authentication and monitoring boundaries
- **GIVEN** metrics are enabled and simple authentication is configured
- **WHEN** a request is rejected by the authentication guard, or `/health` or `/metrics` is requested
- **THEN** neither `mcp_requests_total` nor `mcp_requests_in_flight` changes for that request.

#### Scenario: No active-session gauge is published
- **GIVEN** metrics are enabled
- **WHEN** the `/metrics` payload is scraped
- **THEN** it SHALL NOT publish `mcp_sessions_active`
- **AND** the migration guide SHALL document `mcp_requests_in_flight` as request concurrency, not a session-count alias.

#### Scenario: Disabled metrics preserve protocol behavior
- **GIVEN** metrics remain disabled
- **WHEN** normal HTTP and stdio tool calls execute
- **THEN** the no-op recorder SHALL remain resolvable without enabling collectors or a metrics endpoint
- **AND** the same tool results and business errors SHALL be preserved.
