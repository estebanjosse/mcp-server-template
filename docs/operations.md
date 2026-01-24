# Operations

This document focuses on operating the template’s HTTP and stdio hosts in real environments: running locally, enabling metrics, and deploying via Docker and GitHub Container Registry (GHCR).

## Running the Server

### Stdio Transport (console)

The stdio host is ideal for use with MCP Inspector or other stdio-based MCP clients:

```powershell
# From the repository root
dotnet run --project src/McpServer.Template.Host.Stdio
```

To use MCP Inspector, you can run:

```powershell
npx @modelcontextprotocol/inspector dotnet run --project src/McpServer.Template.Host.Stdio
```

### HTTP Transport (ASP.NET Core)

The HTTP host exposes MCP over HTTP with Server-Sent Events (SSE):

```powershell
# From the repository root
dotnet run --project src/McpServer.Template.Host.Http
```

By default the server listens on `http://localhost:5000`. Useful endpoints include:

- MCP endpoint: `http://localhost:5000/mcp`
- Health endpoint: `http://localhost:5000/health`

You can use `curl` or a browser to verify these endpoints respond as expected.

## Health and Metrics

### Health Endpoint

The HTTP host includes a `/health` endpoint powered by ASP.NET Core health checks. It is designed for:

- Container orchestrators (for example, Kubernetes liveness/readiness probes)
- Load balancers and API gateways
- Monitoring systems and automation scripts

Example call:

```powershell
curl http://localhost:5000/health
```

### Prometheus Metrics

Operational metrics are disabled by default. They can be enabled via configuration or environment variables:

- Configuration key: `Metrics:Enabled`
- Environment variable: `MCP_METRICS_ENABLED`

Example:

```powershell
$env:MCP_METRICS_ENABLED = "true"
dotnet run --project src/McpServer.Template.Host.Http
curl http://localhost:5000/metrics
```

When enabled, the `/metrics` endpoint exposes Prometheus-formatted metrics such as:

- `mcp_requests_total`
- `mcp_tool_invocations_total`
- `mcp_tool_invocations_by_tool_total` (labelled by `tool`)
- `mcp_sessions_active`

Review [src/McpServer.Template.Host.Http/appsettings.json](../src/McpServer.Template.Host.Http/appsettings.json) for configuration examples.

## Docker Deployment

The repository includes a multi-stage Dockerfile for containerizing the HTTP host.

### Building the Image

```powershell
# Build the Docker image from the repo root
docker build -t mcp-server-template .

# Verify the image exists
docker images mcp-server-template
```

The Dockerfile uses:

- A separate build and runtime stage for smaller final images.
- Official Microsoft .NET SDK and ASP.NET runtime images.
- A non-root user for improved security.
- A health check that calls the `/health` endpoint.

### Running the Container

```powershell
# Run the container mapping port 5000
docker run -d -p 5000:5000 --name mcp-server mcp-server-template

# Check health and MCP endpoints
curl http://localhost:5000/health
curl http://localhost:5000/mcp
```

Common options:

- Change host port:
  ```powershell
  docker run -d -p 8080:5000 --name mcp-server mcp-server-template
  ```
- Override environment:
  ```powershell
  docker run -d -p 5000:5000 \
    -e ASPNETCORE_ENVIRONMENT=Production \
    --name mcp-server \
    mcp-server-template
  ```

## Images from GHCR

Pre-built images are published to GitHub Container Registry (GHCR) for this repository.

### Pulling Images

```powershell
# Latest image
docker pull ghcr.io/estebanjosse/mcp-server-template:latest

# Specific version
docker pull ghcr.io/estebanjosse/mcp-server-template:v1.0.0
```

Example run command:

```powershell
docker run -d -p 5000:5000 --name mcp-server ghcr.io/estebanjosse/mcp-server-template:latest
```

Tags typically include:

- `latest` / `main`
- `v<version>` (for example, `v1.0.0`)
- `sha-<commit>` for commit-specific images

For private repositories, authenticate with GHCR before pulling.

## Security Scanning

You can scan local images using Docker tooling such as Docker Scout:

```powershell
docker scout quickview mcp-server-template
docker scout cves mcp-server-template
```

Use these scans to identify and triage vulnerabilities before deploying images to production environments.
