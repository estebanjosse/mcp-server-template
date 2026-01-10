## ADDED Requirements
### Requirement: Metrics Endpoint Availability
The HTTP host MUST expose a `/metrics` endpoint that serves Prometheus text format only when metrics are enabled via configuration.

#### Scenario: Metrics endpoint serves Prometheus format
- **GIVEN** `Metrics:Enabled` is set to `true` (or `MCP_METRICS_ENABLED` is `true`)
- **WHEN** a client sends `GET /metrics`
- **THEN** the server responds with HTTP 200 OK
- **AND** the response `Content-Type` is `text/plain` with Prometheus exposition text (`# HELP` header present)

#### Scenario: Metrics endpoint unavailable when disabled
- **GIVEN** metrics remain disabled by default configuration
- **WHEN** a client sends `GET /metrics`
- **THEN** the server responds with HTTP 404 Not Found because the endpoint is not mapped

### Requirement: Prometheus Middleware Integration
The HTTP host MUST use prometheus-net.AspNetCore middleware to publish built-in ASP.NET metrics when metrics are enabled.

#### Scenario: Built-in HTTP metrics exposed
- **GIVEN** metrics are enabled
- **WHEN** the `/metrics` payload is scraped
- **THEN** it contains ASP.NET metrics such as request duration histograms emitted by prometheus-net.AspNetCore

#### Scenario: Middleware inactive when disabled
- **GIVEN** metrics are disabled
- **WHEN** the application starts
- **THEN** prometheus-net middleware is not registered and no Prometheus collectors are active

### Requirement: MCP Aggregate Metrics
The system MUST publish aggregate counters tracking MCP request flow when metrics are enabled.

#### Scenario: Requests counter increments
- **GIVEN** metrics are enabled
- **WHEN** an MCP request is processed via the HTTP host
- **THEN** a counter named `mcp_requests_total` increases by one

#### Scenario: Tool invocations counter increments
- **GIVEN** metrics are enabled
- **WHEN** any MCP tool invocation completes
- **THEN** a counter named `mcp_tool_invocations_total` increases by one

#### Scenario: Session gauge reflects connections
- **GIVEN** metrics are enabled
- **WHEN** an MCP session starts
- **THEN** a gauge named `mcp_sessions_active` increases by one
- **AND** the gauge decreases when the session ends

### Requirement: MCP Per-Tool Metrics
The system MUST publish per-tool labelled metrics alongside aggregate counters when metrics are enabled.

#### Scenario: Tool-specific counter increments
- **GIVEN** metrics are enabled
- **WHEN** the calculator tool is invoked once
- **THEN** the scrape payload includes `mcp_tool_invocations_total` with a `tool="Calculator"` label incremented by one

#### Scenario: Multiple tools tracked independently
- **GIVEN** metrics are enabled
- **WHEN** both the calculator and greeting tools are invoked
- **THEN** the scrape payload reports separate label values for each tool without mixing counts

### Requirement: Metrics Documentation and Tests
The change MUST document metrics configuration and include automated coverage verifying the scraper output.

#### Scenario: Documentation explains enablement
- **WHEN** an operator reads the project documentation
- **THEN** it explains how to enable metrics via `Metrics:Enabled` or `MCP_METRICS_ENABLED`, defaulting to disabled

#### Scenario: Automated test validates scrape format
- **GIVEN** metrics are enabled in an integration test
- **WHEN** the test fetches `/metrics`
- **THEN** it asserts the response returns Prometheus text containing the custom MCP metrics names
