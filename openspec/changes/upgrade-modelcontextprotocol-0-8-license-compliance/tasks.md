## 1. Upgrade MCP dependencies

- [ ] 1.1 Update `ModelContextProtocolVersion` to `0.8.0-preview.1` in centralized version management.
- [ ] 1.2 Restore and build the solution to confirm package resolution and compile-time compatibility for HTTP and stdio hosts.

## 2. Validate runtime and test compatibility

- [ ] 2.1 Run targeted MCP adapter and host tests (tools/prompts/resources and HTTP host tests) to detect regressions after the upgrade.
- [ ] 2.2 Run full test suite and validate no new failures related to MCP transport registration, `McpException` handling, or protocol type usage.

## 3. Capture breaking-change impact

- [ ] 3.1 Document upstream **BREAKING** changes from 0.8.0-preview.1 (sealed Protocol reference types, `EnumSchema` removal) and confirm whether generated template code is affected.
- [ ] 3.2 Add migration notes for downstream users that may extend/derive Protocol types in their own projects.

## 4. Ensure license and redistribution compliance

- [ ] 4.1 Add or update third-party license/notice documentation for ModelContextProtocol packages, including the Apache-2.0 transition context and historical MIT portions.
- [ ] 4.2 Update contributor/operations docs with redistribution expectations for binaries/containers (license text inclusion and future `NOTICE` propagation if introduced upstream).

## 5. Final verification and release readiness

- [ ] 5.1 Verify documentation links and examples referencing MCP SDK remain correct after upgrade.
- [ ] 5.2 Prepare a concise changelog/release note entry summarizing dependency bump, validation results, and compliance updates.
