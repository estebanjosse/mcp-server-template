# mcp-protocol-compatibility Specification

## Purpose
Define the MCP revisions, transport behavior, and wire contracts supported by the template, and the evidence required to qualify generated hosts.

## Requirements

### Requirement: Explicit protocol and SDK compatibility

The template SHALL target protocol 2026-07-28 and aligned official C# SDK 2.4.0 packages on .NET 10, reconcile required dependency floors and record exact tested client versions. The HTTP host SHALL explicitly select `HttpServerSessionMode.Stateless` at `/mcp`, without a public mode-selection flag or default legacy SSE endpoints. Execution remains subject to the approval checklist.

#### Scenario: Modern HTTP requests without a handshake
- **GIVEN** a generated HTTP host using the approved target and valid authentication
- **WHEN** a modern client sends a conforming capability request without first calling `server/discover` or `initialize`
- **THEN** the endpoint SHALL process per-request metadata without minting or echoing a protocol session ID
- **AND** separate tests SHALL verify discovery, complete tool calls, prompt retrieval and resource reads with exact expected data and protocol revision, not only a non-error HTTP status.

#### Scenario: Down-level clients without persistent HTTP sessions
- **GIVEN** a client uses initialize-handshake revision 2025-11-25 or 2025-06-18
- **WHEN** it initializes and performs normal tool, prompt and resource operations
- **THEN** the default stateless HTTP host SHALL serve version-shaped responses without persistent session requirements
- **AND** a successful silent downgrade SHALL NOT count as modern protocol qualification.

#### Scenario: Removed session endpoints and legacy SSE
- **GIVEN** the default stateless host and otherwise valid authentication
- **WHEN** a client attempts independent GET/DELETE session operations or legacy `/sse` and `/message` transport endpoints
- **THEN** tests SHALL assert exact rejection/status behavior from the pinned SDK rather than a broad allowed-status set
- **AND** POST response SSE SHALL NOT be treated as a legacy transport failure.

### Requirement: Version-aware wire contracts

The migration SHALL validate protocol headers, result discrimination, cache metadata, structured results and error mapping by protocol revision, using the SDK rather than hand-written parallel protocol handling. Test fixtures SHALL supply valid HTTP Accept/content headers except where deliberately testing malformed requests. Exact HTTP status and JSON-RPC code/body expectations SHALL be pinned from the versioned upstream contract before tests are accepted.

#### Scenario: Modern and legacy wire captures
- **GIVEN** the same sample capabilities are exposed to modern and supported down-level clients
- **WHEN** integration tests capture discovery, lists and invocation responses
- **THEN** result discrimination and cache fields SHALL match each protocol era
- **AND** malformed headers, mismatched method/name metadata, missing required metadata and unsupported revisions SHALL each have explicit expected results.

#### Scenario: Structured results and resource errors
- **GIVEN** object, scalar and array fixtures with explicit output schemas and missing-resource requests
- **WHEN** modern and supported down-level clients invoke or read them
- **THEN** schema/result shaping and exact resource-error codes SHALL match the pinned SDK's revision-specific contract
- **AND** legacy clients SHALL NOT receive incompatible modern-only fields.

#### Scenario: Safe cache defaults
- **GIVEN** the scaffold's static welcome and dynamic status resources
- **WHEN** modern clients read them with default cache settings
- **THEN** tests SHALL verify zero-TTL/private defaults and fresh dynamic values
- **AND** no positive-cache feature or public caching of user-specific content SHALL be introduced implicitly.

### Requirement: Real stdio transport and authentication continuity

Both modern and down-level protocol flows SHALL be exercised against real compiled hosts, including official C# client integration and raw wire assertions where needed. Existing authentication modes and monitoring exposure SHALL remain unchanged; `secure` SHALL remain explicitly unimplemented.

#### Scenario: Stdio negotiation, capabilities and shutdown
- **GIVEN** a real stdio host with and without sample tools
- **WHEN** modern discovery or a supported down-level initialize flow performs advertised operations
- **THEN** tests SHALL assert actual tool DTOs/business errors, prompts and resources, and skip unadvertised optional tools rather than sending invalid discovery calls
- **AND** stdout SHALL contain protocol frames only, cancellation SHALL terminate affected work, and EOF shutdown and stream drains SHALL complete within bounded timeouts.

#### Scenario: HTTP guard continuity
- **GIVEN** none/simple authentication and both default/custom API-key header names
- **WHEN** missing, invalid and valid credentials request MCP operations
- **THEN** tests SHALL assert the existing exact authentication outcomes and successful authorized protocol operations
- **AND** monitoring endpoints and invalid secure-mode startup SHALL retain their existing contract.

### Requirement: Generated deliverable validation

The migration SHALL extend the maintained C#/xUnit suites from #66, validate the actual packed template in an isolated hive and build all 12 HTTP-only/stdio-only/dual-host × samples on/off × tests on/off combinations. Repository-only validation SHALL remain outside packages and generated solutions. Superseded Python characterization scripts SHALL be retired only after equivalent replacement coverage is established.

#### Scenario: SDK upgrade acceptance
- **GIVEN** aligned MCP packages and compatible Microsoft dependency resolution
- **WHEN** acceptance is reported
- **THEN** evidence SHALL include source restore/build/test outcomes, new diagnostics and real modern/down-level discovery/invocation over each supported transport
- **AND** every expected generated test assembly SHALL have a unique TRX report with executed tests and no failures/skips/aborts; no-host rejection and legacy/new host-selector compatibility SHALL remain covered.

#### Scenario: Generated hosts without tests
- **GIVEN** a packed candidate generates a host with tests excluded
- **WHEN** the generated deliverable is qualified
- **THEN** external acceptance fixtures SHALL exercise that host's real MCP operations without shipping those fixtures into the generated project
- **AND** package hash/inventory, exact application/client versions, CI head and unrun checks SHALL be recorded; generated Docker execution SHALL NOT be inferred from a successful build matrix.
