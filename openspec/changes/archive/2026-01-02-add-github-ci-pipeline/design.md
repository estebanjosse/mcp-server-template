# Design: GitHub CI Pipeline

**Change ID**: `add-github-ci-pipeline`  
**Related Specs**: `ci-pipeline`

## Overview

This design describes the technical architecture for a GitHub Actions continuous integration pipeline that automates testing, Docker image building, and publishing to GitHub Container Registry (GHCR).

## Architecture

### Workflow Structure

```
┌─────────────────────────────────────────────────────────┐
│                    GitHub Actions CI                     │
│                  (.github/workflows/ci.yml)              │
└─────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────┐   ┌──────────────┐   ┌──────────────┐
│  Test Job     │   │  Build Job   │   │ Publish Job  │
│               │   │              │   │              │
│ - Setup .NET  │   │ - Setup      │   │ - Login to   │
│ - Restore     │   │   Buildx     │   │   GHCR       │
│ - Run Tests   │   │ - Build      │   │ - Push Image │
│ - Report      │   │   Image      │   │ - Tag Image  │
└───────────────┘   └──────────────┘   └──────────────┘
        │                   │                   │
        └───────────────────┴───────────────────┘
                            │
                            ▼
                 ┌────────────────────┐
                 │   GitHub Actions   │
                 │      Status        │
                 │  ✓ Success / ✗ Fail│
                 └────────────────────┘
```

### Job Execution Flow

**Sequential Dependencies**:
```
test → build → publish
```

- **Test Job**: Always runs first; build waits for success
- **Build Job**: Runs after tests pass; builds Docker image
- **Publish Job**: Conditionally runs after build; only for main/tags

**Parallel Option** (alternative):
- Test and Build can run in parallel
- Publish waits for both test AND build to succeed

## Technical Decisions

### 1. Workflow Triggers

**Decision**: Trigger on push, pull_request, and release events

**Rationale**:
- `push`: Catches issues immediately on every commit
- `pull_request`: Validates PRs before merge
- `release`: Enables automated version tagging

**Configuration**:
```yaml
on:
  push:
    branches: [ main, develop ]
    tags: [ 'v*.*.*' ]
  pull_request:
    branches: [ main ]
  release:
    types: [ published ]
```

### 2. Test Execution Strategy

**Decision**: Use `dotnet test` with Release configuration

**Rationale**:
- Release configuration matches production builds
- All test projects discovered automatically by solution file
- Built-in reporting and exit codes

**Configuration**:
```yaml
- name: Restore dependencies
  run: dotnet restore

- name: Build solution
  run: dotnet build --configuration Release --no-restore

- name: Run tests
  run: dotnet test --configuration Release --no-build --verbosity normal
```

**Alternatives Considered**:
- Debug configuration: Rejected; should test production-like builds
- Individual test projects: Rejected; solution-level is simpler

### 3. Docker Build Strategy

**Decision**: Use Docker Buildx with layer caching

**Rationale**:
- Buildx provides BuildKit features (caching, multi-stage optimization)
- GitHub Actions cache significantly speeds up builds
- Layer caching reuses unchanged layers

**Configuration**:
```yaml
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3

- name: Build Docker image
  uses: docker/build-push-action@v5
  with:
    context: .
    file: ./Dockerfile
    push: false
    tags: ${{ steps.meta.outputs.tags }}
    cache-from: type=gha
    cache-to: type=gha,mode=max
```

**Alternatives Considered**:
- Plain docker build: Rejected; slower, no advanced caching
- Docker layer cache action: Rejected; Buildx GHA cache is simpler

### 4. Image Tagging Strategy

**Decision**: Multi-tag strategy based on context

**Rationale**:
- Commit SHA ensures immutability and traceability
- `latest` provides convenience for development
- Semantic versions support production releases
- Branch tags enable feature branch testing

**Tag Rules**:
| Context | Tags Applied |
|---------|--------------|
| PR | `sha-<sha>` only (not pushed) |
| Main branch | `latest`, `sha-<sha>`, `main` |
| Version tag (v1.2.3) | `v1.2.3`, `1.2.3`, `1.2`, `1`, `latest` |
| Other branches | `sha-<sha>`, `<branch-name>` |

**Implementation**:
```yaml
- name: Docker metadata
  id: meta
  uses: docker/metadata-action@v5
  with:
    images: ghcr.io/${{ github.repository }}
    tags: |
      type=ref,event=branch
      type=ref,event=pr
      type=semver,pattern={{version}}
      type=semver,pattern={{major}}.{{minor}}
      type=semver,pattern={{major}}
      type=sha,prefix=sha-
      type=raw,value=latest,enable={{is_default_branch}}
```

### 5. Publishing Strategy

**Decision**: Conditional publishing to GHCR for main branch and tags only

**Rationale**:
- Prevents cluttering registry with PR/feature branch images
- Saves storage and bandwidth
- Main branch represents deployable code
- Tags represent releases

**Configuration**:
```yaml
publish:
  needs: [test, build]
  if: github.event_name != 'pull_request' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v'))
  runs-on: ubuntu-latest
  steps:
    - name: Log in to GHCR
      uses: docker/login-action@v3
      with:
        registry: ghcr.io
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
```

**Alternatives Considered**:
- Publish all branches: Rejected; too many images
- Separate workflow for releases: Rejected; adds complexity

### 6. Authentication Strategy

**Decision**: Use built-in `GITHUB_TOKEN` for GHCR authentication

**Rationale**:
- Automatically available in all workflows
- Scoped to repository permissions
- No manual secret management required
- Follows GitHub best practices

**Permissions Required**:
```yaml
permissions:
  contents: read
  packages: write
```

### 7. Caching Strategy

**Decision**: Use GitHub Actions cache for Docker layers and NuGet packages

**Rationale**:
- Significantly reduces build times (50-70% improvement)
- Free for public repositories, included quota for private
- Native integration with GitHub Actions

**Configuration**:
```yaml
# Docker layer caching (via Buildx)
cache-from: type=gha
cache-to: type=gha,mode=max

# NuGet package caching
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

## Implementation Details

### File Structure
```
.github/
  workflows/
    ci.yml           # Main CI workflow
```

### Runner Environment
- **OS**: `ubuntu-latest` (Linux)
- **Rationale**: Cost-effective, fastest, Docker-native

### Resource Considerations
- **Concurrency**: Limit concurrent runs per branch to save resources
- **Timeout**: Set 30-minute timeout to prevent hung jobs
- **Storage**: Docker images stored in GHCR with retention policy

**Configuration**:
```yaml
concurrency:
  group: ci-${{ github.ref }}
  cancel-in-progress: true

jobs:
  test:
    timeout-minutes: 30
```

## Security Considerations

1. **GITHUB_TOKEN Scope**: Limited to package write; cannot modify code
2. **Docker Image Scanning**: Not included in this phase; can add later
3. **Secrets Exposure**: No secrets hardcoded; use GitHub Secrets for sensitive data
4. **Dependency Pinning**: Pin action versions with SHA for reproducibility

**Best Practices**:
```yaml
# Pin to specific commit SHA
uses: docker/build-push-action@0565240e2d4ab88bba5387d719585280857ece09  # v5.0.0
```

## Performance Optimization

### Expected Build Times
- **First build**: 8-10 minutes (cold cache)
- **Subsequent builds**: 3-5 minutes (warm cache)
- **Test execution**: 1-2 minutes
- **Total workflow**: 5-12 minutes

### Optimization Techniques
1. **Layer Caching**: Reuse unchanged Docker layers
2. **NuGet Caching**: Cache restored packages
3. **Parallel Jobs**: Test and build can run concurrently
4. **BuildKit**: Use BuildKit's parallel layer downloading

## Monitoring and Observability

### Success Metrics
- Workflow success rate (target: >95%)
- Average build time (target: <10 minutes)
- Test pass rate (target: 100%)
- Cache hit rate (target: >80%)

### Failure Handling
- Clear error messages in workflow logs
- Slack/email notifications (future enhancement)
- Auto-retry transient failures (future enhancement)

## Future Enhancements

**Not included in this change** (can be added later):
- Code coverage reporting with Codecov
- Security scanning with Trivy or Snyk
- Multi-architecture builds (arm64 + amd64)
- Automated dependency updates with Dependabot
- Performance benchmarking
- Integration tests with Docker Compose
- Automated changelog generation
- Release automation (GitHub Releases)

## Questions Resolved

1. **Q**: Should workflow run on all branches?  
   **A**: Main + develop for push; all branches for PRs

2. **Q**: Publish from feature branches?  
   **A**: No, only main and version tags

3. **Q**: Semantic version handling?  
   **A**: Automated with docker/metadata-action

4. **Q**: Code coverage?  
   **A**: Deferred to future enhancement

5. **Q**: Scheduled builds?  
   **A**: Not in this phase; can add cron trigger later

## References

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [Docker Metadata Action](https://github.com/docker/metadata-action)
- [OpenSpec Project Conventions](../../project.md)
