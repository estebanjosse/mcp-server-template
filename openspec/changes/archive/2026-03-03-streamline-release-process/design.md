## Context

The repository currently uses a 4-step manual release process:
1. Bump `<Version>` in `McpServer.Template.csproj` via a feature branch + PR
2. Merge to main
3. Push a git tag (`v*.*.*`) to trigger `nuget-publish.yml`
4. Manually create a GitHub Release

The `nuget-publish.yml` workflow already overrides the csproj version with the tag version (`-p:Version=${{ steps.version.outputs.version }}`), making the hardcoded `<Version>` in the csproj redundant for publishing. The `ci.yml` triggers on `release: published` but `nuget-publish.yml` triggers on `push: tags`, creating two separate entry points.

## Goals / Non-Goals

**Goals:**
- Reduce the release process to: merge PR → `gh release create` (single command)
- Make GitHub Release the single source of truth for versioning
- Add auto-generated release notes with PR-based categorization
- Add `<PackageReleaseNotes>` metadata for NuGet.org visibility
- Update documentation to reflect the new process

**Non-Goals:**
- Adopting Conventional Commits enforcement or tooling (e.g., commitlint)
- Automated version bumping (e.g., release-please, NBGV)
- Changelog file generation (`CHANGELOG.md`)
- Changing the CI workflow (`ci.yml`) — it already triggers on `release: published`

## Decisions

### D1: GitHub Release as single trigger for NuGet publish

**Decision**: Change `nuget-publish.yml` trigger from `push: tags: ['v*.*.*']` to `release: types: [published]`.

**Rationale**: A `gh release create v0.0.4-preview` already creates the tag AND the release in one command. By triggering on `release: published`, we avoid the need to push a tag separately. The version extraction still works the same way (from `github.ref` → `refs/tags/v*`).

**Alternative considered**: Keep tag-push trigger and auto-create releases via workflow — adds complexity for no real gain in a solo-maintainer project.

### D2: Placeholder version in csproj

**Decision**: Replace `<Version>0.0.3-preview</Version>` with `<Version>0.0.0-local</Version>`.

**Rationale**: The csproj version is only used for local `dotnet pack` during development. The CI always overrides it. `0.0.0-local` makes it clear this isn't a real version and won't accidentally be pushed to NuGet.

**Alternative considered**: Remove `<Version>` entirely — but MSBuild defaults to `1.0.0` which is misleading.

### D3: Release notes categorization via `.github/release.yml`

**Decision**: Add a `.github/release.yml` file to categorize PRs in auto-generated release notes by label (`enhancement`, `bug`, etc.).

**Rationale**: GitHub's `--generate-notes` flag produces adequate release notes from PR titles. A `release.yml` config adds structure with zero ongoing maintenance cost.

### D4: PackageReleaseNotes URL

**Decision**: Add `<PackageReleaseNotes>https://github.com/estebanjosse/mcp-server-template/releases</PackageReleaseNotes>` to the csproj.

**Rationale**: NuGet.org renders this as a clickable link, giving users access to the changelog without maintaining a separate file.

## Risks / Trade-offs

- **[Risk] Existing automation referencing tag-push** → The only consumer is `nuget-publish.yml` itself; `ci.yml` already uses `release: published`. No other systems depend on the tag-push trigger.
- **[Risk] Local development confusion with `0.0.0-local`** → Mitigated by clear comment in csproj and documentation.
- **[Trade-off] Still manual version selection** → Acceptable for a pre-1.0 solo-maintainer project. Can adopt release-please later if needed.
- **[Trade-off] Release notes quality depends on PR titles** → Already the team's practice. No additional enforcement needed.
