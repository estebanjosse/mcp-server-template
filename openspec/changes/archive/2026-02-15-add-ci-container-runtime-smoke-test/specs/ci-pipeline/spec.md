## MODIFIED Requirements

### Requirement: Docker Image Build

The CI pipeline SHALL build a Docker image from the repository's Dockerfile and SHALL validate container runtime startup before the Docker build stage is considered successful.

#### Scenario: Build image with Docker Buildx
- **GIVEN** the build job is triggered after successful tests
- **WHEN** the Docker build executes
- **THEN** Docker Buildx SHALL be used for the build
- **AND** the existing Dockerfile at repository root SHALL be used
- **AND** the build SHALL complete successfully

#### Scenario: Apply layer caching
- **GIVEN** a Docker image has been built previously
- **WHEN** a subsequent build executes
- **THEN** unchanged Docker layers SHALL be reused from cache
- **AND** build time SHALL be significantly reduced (>50% faster)
- **AND** GitHub Actions cache SHALL be used as the cache backend

#### Scenario: Tag image with commit SHA
- **GIVEN** a Docker image is built
- **WHEN** the build completes
- **THEN** the image SHALL be tagged with `sha-<commit-sha>`
- **AND** the SHA tag SHALL uniquely identify the commit that triggered the build

#### Scenario: Run built image in CI runtime validation
- **GIVEN** a Docker image has been built in the CI build job
- **WHEN** runtime validation starts
- **THEN** the workflow SHALL start a container from the built image
- **AND** the container SHALL expose the HTTP host on the expected CI port mapping

#### Scenario: Validate container readiness through health endpoint
- **GIVEN** the runtime validation container is running
- **WHEN** the workflow probes `GET /health` with retries within a bounded timeout
- **THEN** the workflow SHALL pass runtime validation only after receiving HTTP 200 from `/health`
- **AND** the workflow SHALL fail if readiness is not reached before timeout

#### Scenario: Fail fast and publish diagnostics on runtime startup failure
- **GIVEN** runtime validation has started for the built image
- **WHEN** the container exits before readiness or health checks keep failing until timeout
- **THEN** the workflow SHALL fail the build job
- **AND** the workflow SHALL capture container diagnostics including `docker logs` and `docker inspect`
