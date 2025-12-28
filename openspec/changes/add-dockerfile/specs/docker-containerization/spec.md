# Docker Containerization Specification

**Capability**: `docker-containerization`  
**Status**: Proposed (ADDED)  
**Version**: 1.0.0

## Overview

Provides Docker containerization support for the MCP server HTTP host, enabling deployment in containerized environments with security hardening and build optimization.

---

## ADDED Requirements

### Requirement: Multi-Stage Build Pattern
**Priority**: Must Have  
**Category**: Build Optimization

The Dockerfile SHALL use a multi-stage build pattern with separate stages for build and runtime to minimize final image size.

#### Scenario: Building with multi-stage pattern
**Given** a Dockerfile with build and runtime stages  
**When** the Docker image is built  
**Then** the final image SHALL NOT include .NET SDK or build tools  
**And** the final image SHALL only contain the ASP.NET Core runtime and published application

#### Scenario: Image size optimization
**Given** a multi-stage Dockerfile  
**When** the final image is built  
**Then** the image size SHALL be significantly smaller than a single-stage build including the SDK  
**And** the image SHALL not exceed 250MB (typical .NET 8 runtime + app size)

---

### Requirement: Official .NET Base Images
**Priority**: Must Have  
**Category**: Foundation

The Dockerfile SHALL use official Microsoft .NET Docker images as base images.

#### Scenario: Using SDK image for build stage
**Given** a Dockerfile build stage  
**When** specifying the base image  
**Then** the image SHALL be `mcr.microsoft.com/dotnet/sdk:8.0` or a semantic version thereof

#### Scenario: Using ASP.NET Core runtime for final stage
**Given** a Dockerfile runtime stage  
**When** specifying the base image  
**Then** the image SHALL be `mcr.microsoft.com/dotnet/aspnet:8.0` or a semantic version thereof  
**And** the image SHALL NOT use the full SDK image

---

### Requirement: Non-Root User Execution
**Priority**: Must Have  
**Category**: Security

The containerized application SHALL run as a non-root user to follow security best practices.

#### Scenario: Creating and using non-root user
**Given** a Dockerfile with runtime stage  
**When** the container is run  
**Then** the application process SHALL execute as a non-root user (UID != 0)  
**And** the user SHALL have minimal necessary permissions

#### Scenario: File ownership for non-root user
**Given** application files in the container  
**When** the container starts  
**Then** the application files SHALL be accessible by the non-root user  
**And** the working directory SHALL have appropriate permissions

---

### Requirement: Layer Caching Optimization
**Priority**: Should Have  
**Category**: Build Optimization

The Dockerfile SHALL optimize layer caching to minimize rebuild times when only source code changes.

#### Scenario: Caching NuGet package restore
**Given** a Dockerfile with restore step  
**When** project files are copied before source code  
**Then** the NuGet restore layer SHALL be cached and reused when source code changes  
**And** the restore layer SHALL only be invalidated when project files change

#### Scenario: Separate restore and build steps
**Given** a multi-stage Dockerfile  
**When** building the application  
**Then** the restore step SHALL be separate from the build step  
**And** subsequent builds SHALL skip restore if dependencies haven't changed

---

### Requirement: Docker Ignore File
**Priority**: Must Have  
**Category**: Build Optimization

A `.dockerignore` file SHALL exclude unnecessary files from the Docker build context.

#### Scenario: Excluding build artifacts
**Given** a `.dockerignore` file  
**When** building the Docker image  
**Then** `bin/` and `obj/` directories SHALL be excluded  
**And** test projects SHALL be excluded  
**And** the `.git` directory SHALL be excluded

#### Scenario: Excluding documentation and config files
**Given** a `.dockerignore` file  
**When** building the Docker image  
**Then** README files SHALL be excluded  
**And** `.gitignore` and `.editorconfig` SHALL be excluded  
**And** `.vscode/` and `.vs/` directories SHALL be excluded

---

### Requirement: Port Exposure
**Priority**: Must Have  
**Category**: Networking

The Dockerfile SHALL expose the HTTP server port and support configuration via environment variables.

#### Scenario: Exposing default HTTP port
**Given** a Dockerfile with runtime stage  
**When** the image is built  
**Then** port 5000 SHALL be exposed using `EXPOSE` directive  
**And** the port SHALL be configurable via `ASPNETCORE_URLS` environment variable

#### Scenario: Configuring listening port
**Given** a running container  
**When** `ASPNETCORE_URLS` is set to `http://+:8080`  
**Then** the application SHALL listen on port 8080 instead of the default 5000

---

### Requirement: Minimal Attack Surface
**Priority**: Should Have  
**Category**: Security

The final image SHALL contain only necessary runtime components without development tools, debuggers, or unnecessary packages.

#### Scenario: No SDK tools in runtime image
**Given** a final runtime image  
**When** inspecting the image contents  
**Then** .NET SDK tools SHALL NOT be present  
**And** `dotnet build` and `dotnet restore` commands SHALL NOT be available

#### Scenario: Minimal base image
**Given** a runtime image based on ASP.NET Core runtime  
**When** scanning the image  
**Then** only runtime dependencies SHALL be present  
**And** no package managers (apt, apk) or compilers SHALL be in the PATH

---

### Requirement: Working Directory Convention
**Priority**: Should Have  
**Category**: Convention

The Dockerfile SHALL follow .NET container conventions for working directory structure.

#### Scenario: Using standard /app directory
**Given** a Dockerfile runtime stage  
**When** setting the working directory  
**Then** the working directory SHALL be set to `/app`  
**And** application files SHALL be published to `/app`

---

### Requirement: Build Configuration
**Priority**: Should Have  
**Category**: Build Optimization

The Dockerfile SHALL build the application in Release configuration for production optimization.

#### Scenario: Publishing in Release mode
**Given** a Dockerfile build stage  
**When** running `dotnet publish`  
**Then** the build configuration SHALL be `Release`  
**And** the published output SHALL be optimized for production

#### Scenario: Self-contained or framework-dependent
**Given** a Dockerfile build stage  
**When** publishing the application  
**Then** the application SHALL be published as framework-dependent (not self-contained)  
**And** the runtime image SHALL provide the .NET runtime

---

### Requirement: Documentation for Container Usage
**Priority**: Must Have  
**Category**: Documentation

The project SHALL include documentation for building, running, and configuring the Docker container.

#### Scenario: Building the Docker image
**Given** project documentation  
**When** a developer wants to build the image  
**Then** documentation SHALL include a `docker build` command example  
**And** the command SHALL specify appropriate tags and build arguments

#### Scenario: Running the container
**Given** project documentation  
**When** a developer wants to run the container  
**Then** documentation SHALL include a `docker run` command example  
**And** examples SHALL cover port mapping and environment variables

#### Scenario: Configuration options
**Given** project documentation  
**When** configuring the containerized application  
**Then** documentation SHALL list supported environment variables  
**And** documentation SHALL explain volume mounting for configuration files

---

### Requirement: Health Check Integration
**Priority**: Could Have  
**Category**: Operations

The Dockerfile SHALL support optional health check configuration that can leverage a health endpoint when available.

#### Scenario: Defining container health check
**Given** a Dockerfile with health endpoint available  
**When** adding a `HEALTHCHECK` instruction  
**Then** the health check SHALL query the `/health` endpoint  
**And** the health check SHALL use appropriate interval and timeout values

#### Scenario: Health check without endpoint
**Given** a Dockerfile without a health endpoint  
**When** the container is deployed  
**Then** the Dockerfile SHALL NOT include a `HEALTHCHECK` instruction  
**And** container orchestrators SHALL rely on TCP port checks for liveness

---

## Metadata

**Created**: 2025-12-28  
**Author**: AI Assistant  
**Related Changes**: add-dockerfile  
**Dependencies**: None (complements add-health-endpoint for health checks)
