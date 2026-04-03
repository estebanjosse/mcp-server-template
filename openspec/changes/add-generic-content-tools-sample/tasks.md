## 1. Contracts and service boundaries

- [x] 1.1 Add shared DTOs and request or response contracts for text storage, text search, JSON validation, JSON transformation, HTTP fetch, datetime, and health diagnostics.
- [ ] 1.2 Add Application ports for the new capability groups and register them through the existing DI extension pattern.
- [ ] 1.3 Add configuration contracts for bounded local storage and safe outbound HTTP behavior.

## 2. Content storage and retrieval

- [ ] 2.1 Implement bounded local text storage behind an infrastructure abstraction using server-managed document identifiers.
- [ ] 2.2 Implement application services for `save_text` and `read_text`, including validation, overwrite handling, and not-found behavior.
- [ ] 2.3 Implement application and infrastructure search behavior for `search_text`, including snippets, result limits, and default case-insensitive matching.

## 3. JSON processing

- [ ] 3.1 Implement JSON schema validation service behavior and explicit validation error reporting.
- [ ] 3.2 Implement `transform_json` with the bounded `project` operation and clear handling for unsupported operations or invalid mappings.
- [ ] 3.3 Add application-level tests covering valid input, malformed JSON, schema failures, and transformation failures.

## 4. Runtime helpers and diagnostics

- [ ] 4.1 Implement `get_current_datetime` using the existing clock abstraction and a stable UTC response contract.
- [ ] 4.2 Implement safe outbound HTTP fetch infrastructure with URL validation, timeouts, response size limits, and bounded content-type handling.
- [ ] 4.3 Implement `health_check` diagnostics that report MCP-visible runtime state without duplicating HTTP host `/health` semantics.

## 5. MCP adapter and host integration

- [ ] 5.1 Add MCP tool classes for `save_text`, `read_text`, `search_text`, `validate_json_schema`, `transform_json`, `get_current_datetime`, `http_fetch`, and `health_check`.
- [ ] 5.2 Wire new services into the MCP module registration and record tool-level metrics consistently with existing MCP instrumentation patterns.
- [ ] 5.3 Update host configuration and sample appsettings to support local storage and outbound HTTP policy configuration where required.
- [ ] 5.4 Update `.template.config/template.json` so the generic content sample is fully governed by `--include-sample-tools`, including source files, tests, and sample-facing documentation.
- [ ] 5.5 Remove the legacy `echo` and `calc_divide` sample tool footprint from the opt-in sample set and keep the default scaffold tool-free except for the `Tools/` placeholder.
- [ ] 5.6 Audit every source and test directory that becomes empty when sample content is excluded and preserve them with placeholder files where needed.
- [ ] 5.7 Preserve or refresh placeholder files such as `Tools/.gitkeep` so the tool-free scaffold still guides developers toward where custom tools belong.

## 6. Documentation and verification

- [ ] 6.1 Update sample documentation to explain the new tool set, why it is coherent, and which MCP concepts it demonstrates.
- [ ] 6.2 Refresh "add a new tool" documentation and placeholder examples if the current guidance no longer matches the generated scaffold.
- [ ] 6.3 Add MCP adapter tests for tool contract mapping and error translation.
- [ ] 6.4 Add infrastructure and host-level coverage for storage safety, safe HTTP policy enforcement, and runtime diagnostics exposure.
- [ ] 6.5 Verify generated templates both with and without `--include-sample-tools` so the scaffold stays internally consistent.