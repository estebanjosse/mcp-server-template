## ADDED Requirements

### Requirement: MCP SDK v2 baseline
The template SHALL centralize ModelContextProtocol and ModelContextProtocol.AspNetCore version 2.0.0 for all generated MCP hosts and adapters.

#### Scenario: Generate an HTTP server from the template
- **GIVEN** a developer creates a solution with the HTTP host selected
- **WHEN** the generated solution is restored and built
- **THEN** the MCP adapter and HTTP host SHALL resolve ModelContextProtocol SDK version 2.0.0
- **AND** the solution SHALL build successfully

#### Scenario: Generate a stdio server from the template
- **GIVEN** a developer creates a solution with the stdio host selected
- **WHEN** the generated solution is restored and built
- **THEN** the MCP adapter and stdio host SHALL resolve ModelContextProtocol SDK version 2.0.0
- **AND** the solution SHALL build successfully

### Requirement: Stateless Streamable HTTP endpoint
The HTTP host SHALL expose its configured MCP endpoint through explicitly configured stateless Streamable HTTP transport.

#### Scenario: Process a request without an MCP session identifier
- **GIVEN** the HTTP host is running
- **WHEN** a compatible client sends an MCP request to `/mcp` without a session identifier
- **THEN** the host SHALL process the request according to the negotiated protocol behavior
- **AND** it SHALL NOT reject the request solely because no MCP session identifier is supplied

#### Scenario: Reject an invalid protocol version header
- **GIVEN** the HTTP host is running
- **WHEN** a client sends an MCP request with an invalid `MCP-Protocol-Version` header
- **THEN** the host SHALL return HTTP 400 Bad Request

### Requirement: No legacy session transport by default
The generated HTTP host SHALL NOT enable legacy SSE endpoints or stateful MCP sessions by default.

#### Scenario: Connect using the configured MCP endpoint
- **GIVEN** a generated HTTP host is running with default configuration
- **WHEN** a client connects using the configured `/mcp` endpoint
- **THEN** the host SHALL use stateless Streamable HTTP
- **AND** the client SHALL NOT need to establish or retain a transport session
