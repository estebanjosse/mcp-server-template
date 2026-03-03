## MODIFIED Requirements

### Requirement: Workflow Triggers

The CI workflow MUST be triggered automatically on relevant repository events.

#### Scenario: Trigger on push to main branches
**Given** a push event to the main or develop branch
**When** the push is processed by GitHub
**Then** the CI workflow SHALL execute automatically
**And** all jobs (test, build, publish) SHALL run

#### Scenario: Trigger on pull requests
**Given** a pull request is opened or updated targeting the main branch
**When** the PR is processed by GitHub
**Then** the CI workflow SHALL execute automatically
**And** test and build jobs SHALL run
**And** the publish job SHALL be skipped

#### Scenario: Trigger on version tags
**Given** a version tag matching pattern `v*.*.*` is pushed
**When** the tag is processed by GitHub
**Then** the CI workflow SHALL execute automatically
**And** the publish job SHALL tag the image with the semantic version

#### Scenario: Trigger NuGet publish on GitHub Release
**Given** a GitHub Release is published with a tag matching `v*.*.*`
**When** the release event is processed by GitHub
**Then** the NuGet publish workflow SHALL execute automatically
**And** the version SHALL be extracted from the release tag
**And** a tag-push alone (without a release) SHALL NOT trigger NuGet publishing

---

### Requirement: Release Notes Categorization

The repository MUST provide configuration for auto-generated release notes so that GitHub Releases group changes by type.

#### Scenario: Categorize PRs by label
**Given** a `.github/release.yml` configuration file exists
**When** a release is created with `--generate-notes`
**Then** PRs labeled `enhancement` SHALL appear under a "Features" category
**And** PRs labeled `bug` SHALL appear under a "Bug Fixes" category
**And** remaining PRs SHALL appear under a "Maintenance" category
