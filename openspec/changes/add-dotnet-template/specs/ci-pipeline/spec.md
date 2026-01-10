## ADDED Requirements
### Requirement: Template NuGet Publishing
The CI system MUST publish the dotnet template package to nuget.org only after tests succeed for the release commit.

#### Scenario: Publish template on version tag
- **GIVEN** a semantic version tag (e.g., `v1.3.0`) is pushed
- **AND** the solution tests have passed for that commit
- **WHEN** the template publishing workflow runs
- **THEN** it packs the template into a `.nupkg`
- **AND** pushes the package to nuget.org using the `NUGET_API_KEY` secret

#### Scenario: Skip publishing outside release triggers
- **GIVEN** a pull request or branch push runs CI
- **WHEN** the template publishing workflow evaluates triggers
- **THEN** no NuGet push occurs
- **AND** the workflow reports that publishing is skipped for non-release builds

#### Scenario: Archive published artifacts
- **GIVEN** the template publishing workflow completes
- **WHEN** reviewing CI artifacts for the run
- **THEN** the built `.nupkg` is uploaded as a workflow artifact for traceability
