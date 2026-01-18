## Context
The repository already ships a complete MCP server scaffold with application, infrastructure, MCP adapter, and host projects. Teams currently clone or fork the repository to start new servers, which is slower and risks drifting from upstream improvements. Shipping the scaffold as a dotnet template allows `dotnet new` to generate the solution structure with selectable components. We must also align packaging with existing CI: tests already run, and Docker images publish through the main pipeline. The new template package needs its own distribution flow targeting nuget.org.

## Goals / Non-Goals
- Goals: Deliver a dotnet template installable as `dotnet new mcp-server`, provide opt-in content flags, enforce host selection, produce a NuGet-distributable package, document usage, and automate publishing via CI.
- Non-Goals: Change runtime behaviour of the existing scaffold, redesign repository structure, or automate Git tagging/release management beyond invoking the publishing workflow.

## Decisions
- Template layout mirrors the repository root with symbolic links for optional content; `template.json` will control inclusion via boolean symbols and replacements (e.g., `--include-sample-tools`, `--include-tests`, `--http-host`, `--stdio-host`).
- Use `--name` to substitute the solution, project, and namespace tokens, defaulting to the current `McpServer.Template` when not provided.
- Validation: define a computed symbol that throws an error if both `--http-host` and `--stdio-host` are false, while allowing both true to include both hosts.
- Packaging: run `dotnet new --install` against the locally packed template to verify the scaffold; rely on `dotnet pack` with a `.nuspec` referencing the template artifacts and inheriting version information from `Directory.Build.props`.
- CI: introduce a dedicated GitHub Actions workflow triggered on semantic version tags that depends on the existing test workflow (or re-runs tests) before packing and pushing the NuGet package using a `NUGET_API_KEY` secret.
- Documentation: store packaging and installation guidance under `docs/templates.md` (final name TBD), linking from README once the template ships.

## Alternatives Considered
- Reusing the existing Docker publishing workflow for NuGet distribution was rejected to keep container image release concerns isolated from template packaging.
- Embedding sample tools/tests unconditionally was rejected because teams want a minimal footprint by default.

## Risks / Trade-offs
- CI complexity increases with another workflow; mitigated by isolating template release triggers and keeping steps minimal.
- Template validation must stay in sync with repository structure; include verification scripts and document update steps to limit drift.

## Open Questions
- Should README link directly to the new docs file or wait until the first release? Yes
