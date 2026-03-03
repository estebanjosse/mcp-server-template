## Why

The current release process requires 4 manual steps: bump `<Version>` in the csproj via a PR, merge to main, create a git tag, then manually create a GitHub Release. The version in the csproj is redundant since `nuget-publish.yml` already overrides it from the tag. This change simplifies the flow to a single `gh release create` command that handles tag creation, release notes, and triggers the publish pipeline.

## What Changes

- Replace hardcoded `<Version>` in `McpServer.Template.csproj` with a dev-only placeholder (`0.0.0-local`)
- Add `<PackageReleaseNotes>` pointing to GitHub Releases page
- Change `nuget-publish.yml` trigger from `push: tags` to `release: published` so a GitHub Release is the single entry point
- Add `.github/release.yml` to customize auto-generated release notes categories
- Update `docs/template.md` to document the new simplified release process
- Update `ci-pipeline` spec to reflect the new trigger model

## Capabilities

### New Capabilities

_None — this change modifies existing capabilities only._

### Modified Capabilities

- `ci-pipeline`: NuGet publish trigger changes from tag push to release published event
- `dotnet-template`: Packaging metadata changes (version placeholder, release notes URL)
- `developer-docs`: Release process documentation needs updating in `docs/template.md`

## Impact

- **Workflows**: `nuget-publish.yml` trigger changes — existing tag-push flow will no longer trigger NuGet publish
- **csproj**: `McpServer.Template.csproj` version becomes a placeholder; real version comes exclusively from release/tag
- **New file**: `.github/release.yml` for release notes categorization
- **Docs**: `docs/template.md` "Publishing a New Version" section rewritten
- **No breaking changes** for template consumers — only affects the template repo's own release process
