## Why

The current sample capabilities are intentionally minimal, but they do not yet show a coherent set of MCP tools that developers can treat as a realistic reference when designing production-grade servers. The template needs a generic, non-domain-specific sample that demonstrates useful tool composition, clear contracts, and disciplined backend design without introducing unnecessary complexity.

## What Changes

- Replace the current toy-tool posture with a cohesive content utility sample centered on text persistence, search, JSON processing, and safe runtime helpers.
- Define a file-backed text workflow with `save_text`, `read_text`, and `search_text` using stable document identifiers instead of arbitrary filesystem paths.
- Add JSON-oriented tools with bounded scope: `validate_json_schema` and `transform_json`.
- Add cross-cutting runtime tools: `get_current_datetime`, `http_fetch`, and `health_check`.
- Update the template packaging rules so all sample-related files remain opt-in through `.template.config/template.json` and the existing `--include-sample-tools` flag.
- Document the role of the sample, why the tool set is coherent, and which MCP concepts and backend practices it demonstrates.

## Capabilities

### New Capabilities
- `content-storage-search`: Persist, retrieve, and search plain text documents through stable contracts and bounded local storage.
- `json-processing`: Validate JSON payloads against schemas and perform small, deterministic JSON transformations.
- `runtime-and-fetch`: Provide safe runtime helper tools for current time, outbound HTTP fetch, and MCP-visible health diagnostics.

### Modified Capabilities
- `dotnet-template`: Expand the sample-tools opt-in behavior so `template.json` includes or excludes the full generic sample footprint consistently.

## Impact

- Affected specs: `content-storage-search`, `json-processing`, `runtime-and-fetch`, `dotnet-template`.
- Affected code: `src/McpServer.Template.Contracts/`, `src/McpServer.Template.Application/`, `src/McpServer.Template.Infrastructure/`, `src/McpServer.Template.Mcp/`, HTTP and stdio host wiring where needed, `.template.config/template.json`.
- Affected documentation: `README.md`, `docs/capabilities.md`, `docs/architecture.md`, `docs/development.md`, and sample configuration guidance.
- Affected tests: application, infrastructure, MCP adapter, and host-level coverage for safe HTTP behavior and diagnostics exposure.