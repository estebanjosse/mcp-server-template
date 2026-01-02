# Implementation Tasks: Add GitHub CI Pipeline

**Change ID**: `add-github-ci-pipeline`

## Task Checklist

### Phase 1: Workflow Foundation
- [x] Create `.github/workflows` directory if not exists
- [x] Create `ci.yml` workflow file with basic structure
- [x] Configure workflow triggers (push, pull_request)
- [x] Set up job dependencies and execution flow

### Phase 2: Test Automation
- [x] Add test job with .NET 8 setup
- [x] Configure test execution with `dotnet test`
- [x] Add test failure reporting
- [x] Test the workflow with a sample push

### Phase 3: Docker Build
- [x] Add Docker build job
- [x] Configure Docker Buildx for advanced features
- [x] Implement Docker layer caching
- [x] Define image tagging strategy
- [x] Test Docker build in CI environment

### Phase 4: GHCR Publishing
- [x] Configure GHCR authentication with GITHUB_TOKEN
- [x] Add conditional publishing (main branch only)
- [x] Implement multi-tag pushing (latest, SHA, branch)
- [x] Set up proper image metadata (labels, annotations)
- [x] Test publishing to GHCR

### Phase 5: Validation and Documentation
- [x] Verify workflow runs successfully end-to-end
- [x] Test pull request workflow (should not publish)
- [x] Test main branch workflow (should publish)
- [x] Update README.md with CI badge and instructions
- [x] Document GHCR image usage and pulling instructions

## Task Details

### Task 1: Create `.github/workflows` directory if not exists
**Deliverable**: Directory structure ready for workflow files  
**Validation**: `ls .github/workflows` shows the directory

### Task 2: Create `ci.yml` workflow file with basic structure
**Deliverable**: Basic workflow YAML with name, triggers, and placeholder jobs  
**Validation**: File exists at `.github/workflows/ci.yml` and has valid YAML syntax

### Task 3: Configure workflow triggers (push, pull_request)
**Deliverable**: Workflow triggers defined for relevant events  
**Validation**: Workflow triggers on push and PR events  
**Example**:
```yaml
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]
  release:
    types: [ published ]
```

### Task 4: Set up job dependencies and execution flow
**Deliverable**: Jobs configured with proper dependencies (test → build → publish)  
**Validation**: Jobs run in correct order; publish only runs after successful build

### Task 5: Add test job with .NET 8 setup
**Deliverable**: Test job configured with actions/setup-dotnet@v4  
**Validation**: .NET 8 SDK is available in the job environment

### Task 6: Configure test execution with `dotnet test`
**Deliverable**: Test execution command in workflow  
**Validation**: `dotnet test` runs all test projects and reports results  
**Example**:
```yaml
- name: Run tests
  run: dotnet test --configuration Release --verbosity normal
```

### Task 7: Add test failure reporting
**Deliverable**: Workflow fails and reports errors when tests fail  
**Validation**: Intentionally break a test, verify workflow fails with clear error message

### Task 8: Test the workflow with a sample push
**Deliverable**: Workflow executes successfully in GitHub Actions  
**Validation**: Check Actions tab shows successful run with all test results

### Task 9: Add Docker build job
**Deliverable**: Separate job for building Docker image  
**Validation**: Docker image builds successfully in CI  
**Dependencies**: Test job must pass first

### Task 10: Configure Docker Buildx for advanced features
**Deliverable**: Docker Buildx action configured in workflow  
**Validation**: Build uses buildx for layer caching and optimizations  
**Example**:
```yaml
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v3
```

### Task 11: Implement Docker layer caching
**Deliverable**: Cache configuration for Docker layers  
**Validation**: Second build run is significantly faster than first  
**Example**: Use `actions/cache@v4` or Buildx cache backends

### Task 12: Define image tagging strategy
**Deliverable**: Logic to generate appropriate tags based on context  
**Validation**: Images tagged with SHA, branch, and semantic version (when applicable)  
**Tags**:
- `sha-<commit-sha>` - Always
- `<branch-name>` - For branch builds
- `latest` - For main branch only
- `v<semver>` - For release tags

### Task 13: Test Docker build in CI environment
**Deliverable**: Docker build completes successfully  
**Validation**: Workflow logs show successful image creation with correct tags

### Task 14: Configure GHCR authentication with GITHUB_TOKEN
**Deliverable**: Docker login step using GITHUB_TOKEN  
**Validation**: Workflow can authenticate to ghcr.io  
**Example**:
```yaml
- name: Log in to GHCR
  uses: docker/login-action@v3
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
```

### Task 15: Add conditional publishing (main branch only)
**Deliverable**: Publish job only runs for main branch pushes and releases  
**Validation**: PR builds do not publish; main branch builds do  
**Example**:
```yaml
if: github.event_name != 'pull_request' && (github.ref == 'refs/heads/main' || startsWith(github.ref, 'refs/tags/v'))
```

### Task 16: Implement multi-tag pushing (latest, SHA, branch)
**Deliverable**: Docker push with multiple tags  
**Validation**: GHCR shows image with all expected tags  
**Example**:
```yaml
tags: |
  ghcr.io/${{ github.repository }}:latest
  ghcr.io/${{ github.repository }}:sha-${{ github.sha }}
```

### Task 17: Set up proper image metadata (labels, annotations)
**Deliverable**: Docker images include OCI metadata  
**Validation**: Inspect image shows proper labels (version, commit, build date)  
**Example**: Use `docker/metadata-action@v5`

### Task 18: Test publishing to GHCR
**Deliverable**: Image successfully pushed to GHCR  
**Validation**: Navigate to GitHub Packages, see published image

### Task 19: Verify workflow runs successfully end-to-end
**Deliverable**: Complete workflow run from push to publish  
**Validation**: All jobs pass, image available in GHCR

### Task 20: Test pull request workflow (should not publish)
**Deliverable**: PR workflow runs tests and builds but doesn't publish  
**Validation**: Open a PR, verify workflow runs but publish step is skipped

### Task 21: Test main branch workflow (should publish)
**Deliverable**: Main branch push triggers full workflow with publish  
**Validation**: Push to main, verify image appears in GHCR

### Task 22: Update README.md with CI badge and instructions
**Deliverable**: README shows CI status badge and CI documentation  
**Validation**: Badge is visible and clickable, links to Actions tab  
**Example**:
```markdown
[![CI](https://github.com/USER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/USER/REPO/actions/workflows/ci.yml)
```

### Task 23: Document GHCR image usage and pulling instructions
**Deliverable**: README section explaining how to pull and run images  
**Validation**: Follow documented instructions, able to pull and run image  
**Example**:
```markdown
## Docker Image

Pull the latest image:
```bash
docker pull ghcr.io/USER/REPO:latest
```
```

## Dependencies Between Tasks

```
Phase 1 (Tasks 1-4: Foundation)
    ↓
Phase 2 (Tasks 5-8: Testing)
    ↓
Phase 3 (Tasks 9-13: Docker Build)
    ↓
Phase 4 (Tasks 14-18: Publishing)
    ↓
Phase 5 (Tasks 19-23: Validation & Docs)
```

## Success Criteria

All tasks must be completed and validated before considering this change complete:
1. ✅ Workflow file created and syntactically valid
2. ✅ Tests run automatically on every push and PR
3. ✅ Docker image builds successfully in CI
4. ✅ Images publish to GHCR on main branch
5. ✅ Proper tagging strategy implemented
6. ✅ Documentation updated with CI information
7. ✅ End-to-end workflow tested and working
