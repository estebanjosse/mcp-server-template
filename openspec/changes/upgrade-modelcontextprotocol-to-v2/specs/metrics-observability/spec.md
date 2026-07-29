## MODIFIED Requirements

### Requirement: MCP Aggregate Metrics
The system MUST publish aggregate counters tracking MCP request flow when metrics are enabled and MUST publish a gauge for MCP requests currently in flight rather than persistent MCP sessions.

#### Scenario: Requests counter increments
- **GIVEN** metrics are enabled
- **WHEN** an MCP request is processed via the HTTP host
- **THEN** a counter named `mcp_requests_total` increases by one

#### Scenario: Tool invocations counter increments
- **GIVEN** metrics are enabled
- **WHEN** any MCP tool invocation completes
- **THEN** a counter named `mcp_tool_invocations_total` increases by one

#### Scenario: In-flight request gauge reflects concurrent work
- **GIVEN** metrics are enabled
- **WHEN** an MCP request is executing through the HTTP host
- **THEN** a gauge named `mcp_requests_in_flight` increases while that request is in progress
- **AND** the gauge decreases when that request completes

#### Scenario: No active-session gauge is published
- **GIVEN** metrics are enabled
- **WHEN** the `/metrics` payload is scraped
- **THEN** the payload SHALL NOT publish `mcp_sessions_active`
