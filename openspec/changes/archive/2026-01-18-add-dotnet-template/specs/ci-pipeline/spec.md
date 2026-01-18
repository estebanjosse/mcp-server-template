## ADDED Requirements
### Requirement: Template NuGet Publishing
**Priority**: Must Have  
**Category**: Artifact Distribution

The CI system MUST publish the dotnet template package to nuget.org only after tests succeed for the release commit.

#### Scenario: Publish template on version tag
**Given** a semantic version tag (e.g., `v1.3.0`) is pushed  
**And** the solution tests have passed for that commit  
**When** the template publishing workflow runs  
**Then** the workflow MUST pack the template into a `.nupkg`  
**And** the workflow MUST push the package to nuget.org using the `NUGET_API_KEY` secret

#### Scenario: Skip publishing outside release triggers
**Given** a pull request or branch push runs CI  
**When** the template publishing workflow evaluates triggers  
**Then** no NuGet push SHALL occur  
**And** the workflow SHALL report that publishing is skipped for non-release builds

#### Scenario: Archive published artifacts
**Given** the template publishing workflow completes  
**When** reviewing CI artifacts for the run  
**Then** the built `.nupkg` MUST be uploaded as a workflow artifact for traceability
