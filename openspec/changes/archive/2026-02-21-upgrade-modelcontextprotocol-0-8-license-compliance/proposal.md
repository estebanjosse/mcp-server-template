## Why

The template currently pins `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` to `0.6.0-preview.1`, while the ecosystem has moved to `0.8.0-preview.1` with protocol and transport improvements. Keeping an older preview version increases drift with current MCP clients, examples, and upstream guidance.

In parallel, the SDK introduced a licensing transition (Apache-2.0 for new contributions, with historical MIT portions), so redistribution guidance in this repository should be explicit and actionable.

## What Changes

- Upgrade MCP package versioning from `0.6.0-preview.1` to `0.8.0-preview.1` through centralized version management.
- Validate compatibility across the generated architecture (Mcp adapter, HTTP host, stdio host) and associated tests.
- Add/refresh upgrade validation guidance so maintainers can confidently verify build, runtime transport behavior, and MCP capabilities after the bump.
- Add redistribution guidance for third-party notices/licenses (including MCP SDK license transition context and packaging expectations).
- **BREAKING**: account for upstream 0.8.0 protocol API breaking changes (sealed public Protocol reference types and `EnumSchema` removal), and document expected impact for downstream projects that inherit/extend protocol types.

## Impact

- Affected dependency management: centralized package versions in `Directory.Build.props`.
- Affected runtime surfaces: MCP DI registration and transports in HTTP and stdio hosts.
- Affected tests: MCP adapter tests and host-level validation scenarios that exercise tool/prompt/resource and transport flows.
- Affected documentation/compliance: contributor and distribution docs for third-party license/notice handling.
- Expected outcome: template remains aligned with current MCP C# SDK preview, with explicit migration and redistribution guidance for users and maintainers.
