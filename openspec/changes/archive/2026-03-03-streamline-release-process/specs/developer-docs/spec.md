## MODIFIED Requirements

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
