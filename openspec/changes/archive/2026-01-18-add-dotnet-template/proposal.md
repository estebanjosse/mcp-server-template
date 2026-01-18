# Change: Distribute repository as dotnet new template

## Why
Teams want to bootstrap new MCP servers quickly without cloning this repository manually. Packaging the scaffold as a reusable dotnet template and publishing it to NuGet enables easy installation with `dotnet new mcp-server`.

## What Changes
- Author a dotnet template layout that packages the current scaffold while letting callers opt into sample tools, tests, and chosen host transports.
- Add template parameters (`--include-sample-tools`, `--include-tests`, `--http-host`, `--stdio-host`, `--name`) and enforce host selection so scaffolds are runnable.
- Produce a minimal `.nuspec` and document the packaging/publishing process under `docs/`.
- Extend CI with a NuGet publishing pipeline that builds and pushes the template package after tests succeed.

## Impact
- Affected specs: dotnet-template (new), ci-pipeline
- Affected code: template asset tree (new template root), docs/, .nuspec, GitHub workflow configuration
