# ci-pipeline Specification

## Purpose
TBD - created by archiving change add-github-ci-pipeline. Update Purpose after archive.

## Requirements

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

---

### Requirement: Automated Test Execution

The CI pipeline SHALL execute all unit tests automatically and report results.

#### Scenario: Run all test projects
**Given** the test job is triggered  
**When** tests are executed  
**Then** all test projects in the solution SHALL be discovered and run  
**And** tests SHALL use Release configuration  
**And** test results SHALL be reported in the workflow logs

#### Scenario: Fail workflow on test failures
**Given** one or more tests fail during execution  
**When** the test job completes  
**Then** the workflow SHALL fail with a non-zero exit code  
**And** subsequent jobs (build, publish) SHALL not execute  
**And** the failure SHALL be clearly reported in the workflow summary

#### Scenario: Pass workflow on test success
**Given** all tests pass during execution  
**When** the test job completes  
**Then** the workflow SHALL succeed  
**And** subsequent jobs SHALL be allowed to execute

---

### Requirement: Docker Image Build

The CI pipeline SHALL build a Docker image from the repository's Dockerfile and SHALL validate container runtime startup before the Docker build stage is considered successful.

#### Scenario: Build image with Docker Buildx
**Given** the build job is triggered after successful tests  
**When** the Docker build executes  
**Then** Docker Buildx SHALL be used for the build  
**And** the existing Dockerfile at repository root SHALL be used  
**And** the build SHALL complete successfully

#### Scenario: Apply layer caching
**Given** a Docker image has been built previously  
**When** a subsequent build executes  
**Then** unchanged Docker layers SHALL be reused from cache  
**And** build time SHALL be significantly reduced (>50% faster)  
**And** GitHub Actions cache SHALL be used as the cache backend

#### Scenario: Tag image with commit SHA
**Given** a Docker image is built  
**When** the build completes  
**Then** the image SHALL be tagged with `sha-<commit-sha>`  
**And** the SHA tag SHALL uniquely identify the commit that triggered the build

#### Scenario: Run built image in CI runtime validation
**Given** a Docker image has been built in the CI build job  
**When** runtime validation starts  
**Then** the workflow SHALL start a container from the built image  
**And** the container SHALL expose the HTTP host on the expected CI port mapping

#### Scenario: Validate container readiness through health endpoint
**Given** the runtime validation container is running  
**When** the workflow probes `GET /health` with retries within a bounded timeout  
**Then** the workflow SHALL pass runtime validation only after receiving HTTP 200 from `/health`  
**And** the workflow SHALL fail if readiness is not reached before timeout

#### Scenario: Fail fast and publish diagnostics on runtime startup failure
**Given** runtime validation has started for the built image  
**When** the container exits before readiness or health checks keep failing until timeout  
**Then** the workflow SHALL fail the build job  
**And** the workflow SHALL capture container diagnostics including `docker logs` and `docker inspect`

---

### Requirement: Image Publishing to GHCR

The CI pipeline SHALL publish built Docker images to GitHub Container Registry (GHCR) under specific conditions.

#### Scenario: Publish on main branch push
**Given** a push to the main branch triggers the workflow  
**When** tests and build succeed  
**Then** the publish job SHALL execute  
**And** the image SHALL be pushed to `ghcr.io/<owner>/<repo>`  
**And** the image SHALL be tagged with `latest`, `main`, and `sha-<commit-sha>`

#### Scenario: Skip publishing on pull requests
**Given** a pull request triggers the workflow  
**When** tests and build succeed  
**Then** the publish job SHALL be skipped  
**And** no image SHALL be pushed to GHCR  
**And** the workflow SHALL still report success

#### Scenario: Publish with semantic version on tags
**Given** a version tag `v1.2.3` is pushed  
**When** tests and build succeed  
**Then** the publish job SHALL execute  
**And** the image SHALL be tagged with `v1.2.3`, `1.2.3`, `1.2`, `1`, `latest`, and `sha-<commit-sha>`

---

### Requirement: GHCR Authentication

The CI pipeline SHALL authenticate to GitHub Container Registry securely.

#### Scenario: Authenticate using GITHUB_TOKEN
**Given** the publish job needs to push an image  
**When** authentication is required  
**Then** the built-in `GITHUB_TOKEN` SHALL be used  
**And** no additional secrets SHALL be required  
**And** authentication SHALL succeed with package write permissions

#### Scenario: Fail securely on authentication errors
**Given** GHCR authentication fails  
**When** the publish job attempts to push an image  
**Then** the workflow SHALL fail gracefully  
**And** an error message SHALL be logged  
**And** no partial or incomplete images SHALL be published

---

### Requirement: Image Tagging Strategy

The CI pipeline SHALL apply multiple meaningful tags to Docker images based on build context.

#### Scenario: Apply branch-based tags
**Given** a build is triggered from a named branch  
**When** the image is tagged  
**Then** the image SHALL be tagged with the branch name  
**And** the tag SHALL be sanitized for Docker compatibility

#### Scenario: Apply semantic version tags
**Given** a build is triggered from a version tag `v1.2.3`  
**When** the image is tagged  
**Then** the image SHALL be tagged with `1.2.3` (full version)  
**And** the image SHALL be tagged with `1.2` (minor version)  
**And** the image SHALL be tagged with `1` (major version)  
**And** the image SHALL be tagged with `latest`

#### Scenario: Apply latest tag to default branch only
**Given** a build completes on a non-default branch  
**When** the image is tagged  
**Then** the image SHALL NOT be tagged with `latest`  
**And** only the default branch builds SHALL receive the `latest` tag

---

### Requirement: Build Performance Optimization

The CI pipeline SHALL optimize build times through caching and parallelization.

#### Scenario: Cache NuGet packages
**Given** NuGet packages have been restored previously  
**When** a subsequent workflow runs  
**Then** restored packages SHALL be loaded from cache  
**And** package restoration time SHALL be significantly reduced

#### Scenario: Complete workflow within time budget
**Given** a typical code change is pushed  
**When** the full CI workflow executes  
**Then** the workflow SHALL complete within 15 minutes for cold cache  
**And** the workflow SHALL complete within 8 minutes for warm cache

---

### Requirement: Workflow Status Reporting

The CI pipeline SHALL provide clear status reporting for all workflow executions.

#### Scenario: Display workflow status badge
**Given** the CI workflow exists  
**When** viewed in the repository README  
**Then** a workflow status badge SHALL be displayed  
**And** the badge SHALL show the current status (passing/failing)  
**And** the badge SHALL link to the workflow runs

#### Scenario: Report clear error messages
**Given** a workflow job fails  
**When** viewing the workflow logs  
**Then** the failure reason SHALL be clearly stated  
**And** relevant log output SHALL be visible  
**And** the failed step SHALL be highlighted

---

### Requirement: Image Metadata

Published Docker images SHALL include metadata for traceability and identification.

#### Scenario: Include OCI labels
**Given** a Docker image is built and published  
**When** the image metadata is inspected  
**Then** OCI standard labels SHALL be present  
**And** labels SHALL include commit SHA, build date, and version  
**And** labels SHALL include repository URL and documentation link

#### Scenario: Enable image inspection
**Given** an image is pulled from GHCR  
**When** inspecting the image with `docker inspect`  
**Then** all metadata labels SHALL be visible  
**And** the source commit SHALL be identifiable

---

### Requirement: Workflow Concurrency Control

The CI pipeline SHALL manage concurrent workflow executions to prevent resource conflicts.

#### Scenario: Cancel redundant workflow runs
**Given** multiple commits are pushed rapidly to the same branch  
**When** a new workflow run starts  
**Then** in-progress workflow runs for the same branch SHALL be cancelled  
**And** only the latest workflow run SHALL complete  
**And** resources SHALL not be wasted on outdated builds

#### Scenario: Isolate pull request workflows
**Given** multiple pull requests exist simultaneously  
**When** workflows are triggered  
**Then** each PR workflow SHALL run independently  
**And** PR workflows SHALL not cancel each other

---

### Requirement: Template NuGet Publishing

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

---

