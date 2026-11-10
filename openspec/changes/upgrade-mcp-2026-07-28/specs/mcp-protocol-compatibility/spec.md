## ADDED Requirements

### Requirement: Explicit protocol and SDK compatibility

The template SHALL identify its targeted stable protocol revision, SDK version and validated client matrix, and SHALL use an explicit HTTP session policy instead of inheriting an SDK default.

#### Scenario: Modern HTTP request
- **GIVEN** a newly generated HTTP host targets protocol 2026-07-28
- **WHEN** a conforming client discovers and calls the server
- **THEN** the SDK-backed endpoint SHALL serve modern request metadata without a protocol session ID
- **AND** tests SHALL verify the selected revision and complete tool, prompt and resource operations rather than only asserting a non-error status.

#### Scenario: Down-level and session-dependent clients
- **GIVEN** a client uses an explicitly supported initialize-handshake revision
- **WHEN** it connects to the chosen stateless or hybrid mode
- **THEN** tests SHALL verify the documented behavior for that mode
- **AND** a session-dependent client SHALL use the documented hybrid/stateful legacy path rather than being represented as a modern stateful HTTP connection.

### Requirement: Version-aware wire contracts

The migration SHALL validate protocol headers, result discrimination, cache metadata, structured results and error mapping by negotiated protocol revision, using the SDK rather than hand-written parallel protocol handling.

#### Scenario: Modern and legacy wire captures
- **GIVEN** the same sample capabilities are exposed to modern and supported down-level clients
- **WHEN** integration tests capture their responses
- **THEN** each response SHALL match its protocol-era contract
- **AND** malformed headers, missing required metadata and unsupported revisions SHALL have explicit expected results.

### Requirement: Generated deliverable validation

The migration SHALL validate the actual packed template and representative generated HTTP-only, stdio-only and dual-host projects, both with and without sample tools and tests.

#### Scenario: SDK upgrade acceptance
- **GIVEN** a migration candidate uses aligned MCP packages and compatible Microsoft dependencies
- **WHEN** acceptance is reported
- **THEN** evidence SHALL include restore/build/test outcomes and real discovery/invocation over each supported transport
- **AND** skipped or unrun checks SHALL not be represented as passing.
