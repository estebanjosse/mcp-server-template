# Add Dockerfile

**Change ID**: `add-dockerfile`  
**Status**: Proposed  
**Created**: 2025-12-28

## Problem Statement

The MCP server template currently lacks Docker containerization support, making it difficult to:
- Deploy the HTTP host (`McpServer.Template.Host.Http`) in containerized environments (Kubernetes, Docker Swarm, cloud platforms)
- Ensure consistent runtime environments across development, testing, and production
- Leverage container orchestration features (scaling, health checks, rolling updates)
- Package and distribute the server as a portable, self-contained artifact
- Implement security best practices for production deployments (non-root users, minimal attack surface)

Modern .NET applications typically provide Dockerfiles that follow best practices for multi-stage builds, layer caching, security hardening, and size optimization.

## Proposed Solution

Add a secure and optimized Dockerfile that:
1. Uses **multi-stage build** pattern to minimize final image size
2. Targets the **HTTP host** (`McpServer.Template.Host.Http`) as the primary containerized entrypoint
3. Uses official Microsoft .NET images (SDK for build, ASP.NET Core runtime for final stage)
4. Implements **security best practices**:
   - Non-root user execution
   - Minimal base image (runtime-only, not SDK)
   - No unnecessary packages or tools
   - Read-only filesystem where possible
5. Optimizes for **layer caching** and build performance:
   - Copy project files before source code to cache NuGet restore
   - Separate restore and build steps
   - Leverage BuildKit cache mounts (optional)
6. Includes `.dockerignore` to exclude build artifacts, tests, and unnecessary files
7. Exposes standard HTTP port (5000) with configurable environment variables
8. Provides clear documentation for building and running the container

## Scope

### In Scope
- Multi-stage Dockerfile for `McpServer.Template.Host.Http`
- `.dockerignore` file to optimize build context
- Docker-specific configuration (environment variables, port exposure)
- Documentation for building, running, and configuring the container
- Security hardening (non-root user, minimal base image)
- Build optimization (layer caching, multi-stage pattern)

### Out of Scope
- Dockerizing the stdio host (stdio transport not typically containerized)
- Docker Compose configuration (can be added in future iteration)
- Kubernetes manifests or Helm charts
- Container registry publishing workflows
- Advanced orchestration features (service mesh, sidecars)
- Windows container support (Linux containers only)

## Affected Capabilities
- **docker-containerization** (NEW): Provides secure, optimized Docker containerization for the HTTP host

## Dependencies
- None (no blocking dependencies, but complements the `add-health-endpoint` change for container health checks)

## Risks and Mitigation
- **Risk**: Dockerfile may become outdated as .NET versions evolve
  - **Mitigation**: Use version variables and document update process; pin to .NET 8 with clear upgrade path
- **Risk**: Build times may increase for developers not using Docker
  - **Mitigation**: Docker is opt-in; local development workflow unchanged
- **Risk**: Security vulnerabilities in base images
  - **Mitigation**: Use official Microsoft images, document image scanning practices, enable Dependabot for base image updates

## Alternatives Considered
1. **No Dockerfile**: Current approach; limits deployment flexibility and production-readiness
2. **Single-stage Dockerfile**: Simpler but results in larger images (includes SDK); rejected for production inefficiency
3. **Alpine-based images**: Smaller but .NET support is less mature; use official Debian-based runtime for stability
4. **Separate Dockerfile per host**: Unnecessary duplication; HTTP host is primary containerization target

## Success Criteria
- Dockerfile successfully builds without errors
- Final image size is optimized (runtime-only, minimal layers)
- Container runs as non-root user
- HTTP server accessible on exposed port (5000)
- `.dockerignore` excludes unnecessary files (tests, bin/, obj/, .git)
- Documentation includes build, run, and configuration examples
- Build leverages layer caching for faster subsequent builds
- Security scanning tools (e.g., Docker Scout, Trivy) show no critical vulnerabilities in application layer
