# metrics-observability Specification

## Purpose
Define the optional Prometheus endpoint and metrics for HTTP request concurrency and MCP tool activity.

## Requirements

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
