## 1. Template Assets
- [x] 1.0 Migrate to slnx project solution
- [x] 1.1 Create template configuration (template.json, content mapping, symbols) that mirrors the current repository structure while excluding build artifacts.
- [x] 1.2 Implement template symbols for `--include-sample-tools`, `--include-tests`, `--http-host`, `--stdio-host`, and `--name`, including validation that fails when neither host option is supplied and supports selecting both.
- [x] 1.3 Author automation or manual verification scripts that run `dotnet new mcp-server` with common flag combinations to confirm the scaffold restores, builds, and runs.

## 2. Packaging & Documentation
- [x] 2.1 Write documentation under `docs/` that explains the template layout, parameters, packaging workflow, and installation commands.
- [ ] 2.2 Create a minimal `.nuspec` that sources metadata (ID, version, authors) from existing repository versioning and references the generated template artifacts.

## 3. CI Publishing Pipeline
- [ ] 3.1 Extend GitHub Actions so the NuGet release pipeline runs after tests succeed (e.g., on version tags) and builds the template package.
- [ ] 3.2 Configure the workflow to push the `.nupkg` to nuget.org using a repository secret for the API key and publish the artifact for traceability.
