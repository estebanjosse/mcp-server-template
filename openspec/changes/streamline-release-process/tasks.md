## 1. NuGet Publish Workflow

- [ ] 1.1 Change `nuget-publish.yml` trigger from `push: tags: ['v*.*.*']` to `release: types: [published]`
- [ ] 1.2 Verify version extraction still works from `github.ref` (refs/tags/v* is set on release events too)

## 2. Package Metadata

- [ ] 2.1 Replace `<Version>0.0.3-preview</Version>` with `<Version>0.0.0-local</Version>` in `McpServer.Template.csproj`
- [ ] 2.2 Update the version comment to explain it's a local placeholder overridden by CI
- [ ] 2.3 Add `<PackageReleaseNotes>https://github.com/estebanjosse/mcp-server-template/releases</PackageReleaseNotes>` to the csproj PropertyGroup

## 3. Release Notes Configuration

- [ ] 3.1 Create `.github/release.yml` with categories: Features (`enhancement`), Bug Fixes (`bug`), Maintenance (catch-all)

## 4. Documentation

- [ ] 4.1 Update `docs/template.md` "Publishing a New Version" section to describe the single-step `gh release create` flow
- [ ] 4.2 Update `docs/template.md` "NuGet Publish Workflow" section to reflect the new `release: published` trigger
- [ ] 4.3 Remove references to manual version bumping in `docs/template.md`
