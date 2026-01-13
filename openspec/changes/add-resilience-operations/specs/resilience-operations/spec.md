## ADDED Requirements
### Requirement: Readiness Endpoint
The HTTP host SHALL expose a readiness endpoint distinct from `/health` to signal when the service is ready to receive traffic.

#### Scenario: Ready state after dependencies initialized
- **GIVEN** all required services and background initializers have completed
- **WHEN** a client calls `/ready`
- **THEN** the endpoint responds with HTTP 200 OK
- **AND** it returns non-200 while dependencies are still initializing

### Requirement: Graceful Shutdown Handling
The application SHALL drain in-flight requests and sessions before process termination within a configurable timeout.

#### Scenario: Shutdown waits for active sessions
- **GIVEN** the server receives a shutdown signal
- **WHEN** MCP sessions are active
- **THEN** the server waits up to the configured shutdown timeout to allow completion
- **AND** it stops accepting new `/mcp` connections during the drain

### Requirement: Request Timeout and Back-pressure Controls
The system SHALL enforce configurable timeouts and concurrency limits to prevent resource exhaustion.

#### Scenario: Long-running request exceeds timeout
- **GIVEN** a request exceeds the configured execution timeout
- **WHEN** the timeout elapses
- **THEN** the server cancels the request and returns HTTP 504 Gateway Timeout with an informative payload

#### Scenario: Concurrent session limit enforced
- **GIVEN** the concurrent session limit is set to `N`
- **WHEN** additional clients attempt to open sessions beyond the limit
- **THEN** the server rejects new sessions with HTTP 429 or queues them per configuration

### Requirement: SSE Keep-alive Support
The HTTP host SHALL emit periodic SSE keep-alive events for long-lived streams when enabled.

#### Scenario: Keep-alive prevents idle disconnects
- **GIVEN** an SSE connection remains idle beyond the keep-alive interval
- **WHEN** the interval elapses
- **THEN** the server sends a comment event to maintain the connection
- **AND** the interval is configurable via configuration settings

### Requirement: Deployment Resilience Documentation
Documentation SHALL outline deployment drain procedures, timeout tuning, and recommended readiness probe configuration.

#### Scenario: Drain procedure documented
- **GIVEN** an operator prepares a rolling deployment
- **WHEN** consulting the resilience guide
- **THEN** the guide lists steps to take the service out of rotation, wait for readiness to clear, and monitor shutdown completion
