# Implementation Tasks: Add Dockerfile

**Change ID**: `add-dockerfile`

## Tasks

- [x] **Create Dockerfile with multi-stage build**
  - Add build stage using `mcr.microsoft.com/dotnet/sdk:8.0`
  - Copy solution and project files for NuGet restore
  - Restore dependencies with `dotnet restore`
  - Copy source code and build with `dotnet build --configuration Release`
  - Publish application with `dotnet publish --configuration Release --output /app/publish`
  - Add runtime stage using `mcr.microsoft.com/dotnet/aspnet:8.0`
  - Set working directory to `/app`
  - Copy published output from build stage
  - Create non-root user and switch to it
  - Expose port 5000
  - Set entrypoint to run `McpServer.Template.Host.Http.dll`
  - **Validation**: Run `docker build -t mcp-server-template .` successfully
  - **Validation**: Verify final image size is under 250MB with `docker images`

- [x] **Create .dockerignore file**
  - Exclude build artifacts (`bin/`, `obj/`)
  - Exclude test projects (`tests/`, `*.Tests/`)
  - Exclude version control (`.git/`, `.gitignore`, `.gitattributes`)
  - Exclude IDE configurations (`.vscode/`, `.vs/`, `*.user`, `*.suo`)
  - Exclude documentation (`*.md`, `LICENSE`, `openspec/`)
  - Exclude solution files (`*.sln`)
  - **Validation**: Build Docker image and verify excluded files are not in build context (check build output size)

- [x] **Test Docker container locally**
  - Run container: `docker run -p 5000:5000 mcp-server-template`
  - Verify HTTP server starts and listens on port 5000
  - Test MCP endpoint: `curl http://localhost:5000/mcp`
  - Verify container runs as non-root user: `docker exec <container-id> whoami`
  - Test with custom port: `docker run -p 8080:8080 -e ASPNETCORE_URLS=http://+:8080 mcp-server-template`
  - **Validation**: All HTTP requests succeed, non-root user confirmed

- [x] **Add Docker documentation to README**
  - Add "🐳 Docker Deployment" section after "🎮 Running the Server"
  - Include build command with example: `docker build -t mcp-server-template .`
  - Include run command with port mapping: `docker run -d -p 5000:5000 --name mcp-server mcp-server-template`
  - Document environment variables (ASPNETCORE_URLS, ASPNETCORE_ENVIRONMENT)
  - Add example for custom configuration: mounting `appsettings.json`
  - Add example for stopping and removing containers
  - Include troubleshooting tips (checking logs with `docker logs`)
  - **Validation**: Follow documented steps to build and run container successfully

- [x] **(Optional) Add HEALTHCHECK to Dockerfile**
  - If `add-health-endpoint` change is implemented first, add `HEALTHCHECK` instruction
  - Use `curl` or `wget` to query `/health` endpoint
  - Set reasonable interval (30s), timeout (3s), retries (3)
  - **Validation**: Check container health status with `docker ps` shows "healthy"
  - **Note**: Skip if health endpoint not yet implemented; can be added later

- [x] **Security scan the Docker image**
  - Run `docker scout quickview mcp-server-template` or `trivy image mcp-server-template`
  - Review scan results for critical/high vulnerabilities
  - Document any findings and mitigation steps
  - **Validation**: No critical vulnerabilities in application layer

- [x] **Test layer caching optimization**
  - Make a source code change in a service file
  - Rebuild the Docker image
  - Verify that NuGet restore layer is reused (cached)
  - Check build time is significantly faster than initial build
  - **Validation**: Cached restore layer shown in build output, build time reduced by >50%

## Dependencies
- None (can be implemented independently)
- Complements `add-health-endpoint` for health check integration (optional)

## Notes
- Tasks should be completed sequentially for best results
- Test after each major task to catch issues early
- If health endpoint is not available, skip the HEALTHCHECK task and add it later
- Consider testing with different ASP.NET Core environment configurations (Development, Production)
