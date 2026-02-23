## Why

The template currently targets `ModelContextProtocol`/`ModelContextProtocol.AspNetCore` `0.8.0-preview.1`, while upstream released `0.9.0-preview.1` with breaking API and transport behavior updates before the planned 1.0 stabilization. Staying on 0.8 increases drift from current SDK semantics and can hide migration risks for users who generate servers from this template.

## What Changes

- Upgrade centralized MCP package version management from `0.8.0-preview.1` to `0.9.0-preview.1`.
- Validate build and test compatibility for the MCP adapter and both hosts (stdio + HTTP).
- Add targeted compatibility checks for 0.9 behavior shifts in Streamable HTTP (protocol-version header validation and stricter session requirements for non-initialize POST requests).
- Review and document impact of key 0.9 API changes on the generated code and extension guidance:
  - binary payload API changes (`ReadOnlyMemory<byte>`, factory/property renames),
  - request/message filter registration model changes,
  - handler options registration changes,
  - protected resource metadata URI string typing,
  - `McpErrorCode` behavior alignment.
- Update contributor/operations docs with migration and verification guidance for downstream users.

## Impact

- Affected dependency management: `Directory.Build.props` and all projects consuming `$(ModelContextProtocolVersion)`.
- Affected runtime surfaces: MCP registration in HTTP and stdio hosts, HTTP `/mcp` behavior under stricter protocol/session validation.
- Affected tests: MCP adapter tests and HTTP host tests; likely additions for header/session compatibility behavior.
- Affected documentation: development/operations/changelog notes for migration and release readiness.
- **BREAKING**: downstream projects that directly use changed 0.9 APIs (binary content members, filter configuration extensions, handler options patterns, URI-typed protected resource metadata) may require code updates even if this template itself remains largely composition-based.
