## 1. Upgrade MCP dependencies

- [x] 1.1 Update `ModelContextProtocolVersion` to `0.8.0-preview.1` in centralized version management.
- [x] 1.2 Restore and build the solution to confirm package resolution and compile-time compatibility for HTTP and stdio hosts.

## 2. Validate runtime and test compatibility

- [x] 2.1 Run targeted MCP adapter and host tests (tools/prompts/resources and HTTP host tests) to detect regressions after the upgrade.
- [x] 2.2 Run full test suite and validate no new failures related to MCP transport registration, `McpException` handling, or protocol type usage.

## 3. Capture breaking-change impact

- [x] 3.1 Document upstream **BREAKING** changes from 0.8.0-preview.1 (sealed Protocol reference types, `EnumSchema` removal) and confirm whether generated template code is affected.

## 4. Ensure license and redistribution compliance

- [x] 4.1 Add or update third-party license/notice documentation for ModelContextProtocol packages, including the Apache-2.0 transition context and historical MIT portions.
- [x] 4.2 Update contributor/operations docs with redistribution expectations for binaries/containers (license text inclusion and future `NOTICE` propagation if introduced upstream).

## 5. Final verification and release readiness

- [x] 5.1 Verify documentation links and examples referencing MCP SDK remain correct after upgrade.
