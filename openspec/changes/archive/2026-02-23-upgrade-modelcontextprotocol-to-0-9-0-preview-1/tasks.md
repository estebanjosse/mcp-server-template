## 1. Upgrade MCP dependency and compile baseline

- [x] 1.1 Update `ModelContextProtocolVersion` to `0.9.0-preview.1` in centralized version management.
- [x] 1.2 Restore and build the full solution to confirm package resolution and compile-time compatibility across MCP adapter and both hosts.

## 2. Validate runtime and regression compatibility

- [x] 2.1 Run targeted MCP adapter and HTTP host tests (tools/prompts/resources + host tests) to detect immediate regressions after the bump.
- [x] 2.2 Run full test suite and verify no new failures related to MCP registration, transport startup, and `McpException` behavior.

## 3. Cover 0.9 HTTP behavior changes

- [x] 3.1 Add/update HTTP host tests for `MCP-Protocol-Version` handling (invalid/unsupported value returns 400; missing header still accepted).
- [x] 3.2 Add/update HTTP host tests for stricter stateful Streamable HTTP session behavior (non-initialize POST without session id), and decide/document whether compat switch is needed.

## 4. Assess and document breaking API impact

- [x] 4.1 Review template source for 0.9 breaking API surfaces (binary payload types, filter registration API, handler options model, protected resource metadata URI typing, error-code alignment) and capture findings.

## 5. Finalize release readiness documentation

- [x] 5.1 Verify MCP SDK documentation/release links used in repository docs remain valid and update references if needed.
