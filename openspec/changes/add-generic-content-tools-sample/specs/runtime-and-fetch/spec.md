## ADDED Requirements

### Requirement: Current UTC time is available as a simple runtime tool
The system MUST provide a `get_current_datetime` tool that returns the current server time in UTC using a stable, explicit response format.

#### Scenario: Current UTC time is returned
- **WHEN** a client invokes `get_current_datetime`
- **THEN** the system returns a UTC date-time value
- **AND** the system returns a Unix timestamp representation

### Requirement: Outbound HTTP fetch is read-only and bounded
The system MUST provide an `http_fetch` tool for retrieving remote text or JSON content using a constrained read-only policy.

#### Scenario: Fetch remote JSON successfully
- **WHEN** a client invokes `http_fetch` with an allowed URL that returns JSON within configured limits
- **THEN** the system returns the response status code
- **AND** the system returns the response content type
- **AND** the system returns the fetched content

#### Scenario: Reject unsupported URL scheme
- **WHEN** a client invokes `http_fetch` with a URL that uses an unsupported scheme
- **THEN** the system rejects the request with a validation error

#### Scenario: Reject oversized or truncated-safe response
- **WHEN** a client invokes `http_fetch` and the response exceeds the configured content size limit
- **THEN** the system either rejects the request or returns a bounded truncated response according to configured sample policy
- **AND** the result clearly indicates whether truncation occurred

#### Scenario: Reject disallowed network target
- **WHEN** a client invokes `http_fetch` with a URL targeting a disallowed host or address range
- **THEN** the system rejects the request before issuing the outbound request

#### Scenario: Timeout is surfaced clearly
- **WHEN** a client invokes `http_fetch` and the remote server does not respond within the configured timeout
- **THEN** the system fails the request with a timeout error

### Requirement: MCP-visible diagnostics are distinct from host operational endpoints
The system MUST provide a `health_check` tool that exposes MCP-relevant diagnostics without redefining the HTTP host `/health` endpoint behavior.

#### Scenario: Health check reports runtime diagnostics
- **WHEN** a client invokes `health_check`
- **THEN** the system returns a diagnostic payload containing server status, version, uptime, and current timestamp

#### Scenario: Health check reports capability state
- **WHEN** a client invokes `health_check`
- **THEN** the system indicates whether local text storage is available
- **AND** the system indicates whether outbound HTTP fetch is enabled or restricted by configuration

#### Scenario: HTTP host health endpoint remains separate
- **WHEN** the HTTP host exposes `/health` and an MCP client invokes `health_check`
- **THEN** the MCP tool result is defined by MCP capability contracts
- **AND** the HTTP endpoint remains defined by host operational health behavior