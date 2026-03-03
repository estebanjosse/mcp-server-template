# developer-docs Specification

## Purpose
TBD - created by archiving change refactor-docs-onboarding. Update Purpose after archive.
## Requirements
### Requirement: Onboarding Landing Page
The repository SHALL provide a root-level README focused on onboarding new users by summarizing prerequisites, installation steps, configuration, and quick start instructions for both stdio and HTTP hosts while deferring deep-dive content.

#### Scenario: Quick start guidance
- **GIVEN** a developer unfamiliar with the template
- **WHEN** they open the root README
- **THEN** they can identify prerequisites, installation commands, configuration notes, and execution steps for stdio and HTTP hosts within the primary sections.

### Requirement: Dedicated Documentation Directory
The repository SHALL expose detailed architecture, capability, and operations information via markdown files stored under a docs/ directory instead of embedding them in the root README.

#### Scenario: Sectional documentation discovery
- **WHEN** a user navigates into docs/
- **THEN** they find separate markdown files covering architecture overview, feature and capability reference, and operational procedures.

#### Scenario: Release process documentation
- **WHEN** a contributor opens `docs/template.md`
- **THEN** the "Publishing a New Version" section SHALL describe a single-step release process using `gh release create`
- **AND** the documentation SHALL explain that the version in `McpServer.Template.csproj` is a local placeholder overridden by CI
- **AND** the documentation SHALL list the exact `gh release create` command with `--generate-notes` flag
- **AND** the old multi-step process (manual version bump → PR → tag → release) SHALL be removed

### Requirement: Cross-linked Documentation Navigation
The onboarding README SHALL link to the corresponding docs/ pages so that advanced readers can access deep-dive material directly from the landing page.

#### Scenario: README deep dive handoff
- **WHEN** the README references architecture, capabilities, or operations
- **THEN** it provides hyperlinks to the associated docs/ files located under docs/.

