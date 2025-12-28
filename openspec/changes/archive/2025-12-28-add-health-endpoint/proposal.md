# Add Health Endpoint

**Change ID**: `add-health-endpoint`  
**Status**: Proposed  
**Created**: 2025-12-28

## Problem Statement

The HTTP host (`McpServer.Template.Host.Http`) currently lacks a standard health check endpoint. This makes it difficult to:
- Monitor server availability in production environments
- Integrate with container orchestrators (Kubernetes, Docker Compose) for liveness/readiness probes
- Implement load balancer health checks
- Diagnose whether the server is running and responding to requests

Modern web APIs typically expose health endpoints (e.g., `/health`, `/healthz`) that return simple status indicators for monitoring and orchestration tools.

## Proposed Solution

Add a health endpoint to the HTTP host that:
1. Integrates ASP.NET Core health check middleware (`Microsoft.Extensions.Diagnostics.HealthChecks`)
2. Exposes a `/health` route using `MapHealthChecks`
3. Returns HTTP 200 OK with a JSON response indicating the server is healthy
4. Does not interfere with existing MCP endpoint functionality
5. Requires no authentication/authorization (standard for health checks)
6. Is lightweight and suitable for frequent polling by monitoring systems
7. Provides extensibility for adding custom health checks in future iterations

## Scope

### In Scope
- Add `/health` endpoint to `McpServer.Template.Host.Http`
- Return JSON response with basic health status
- Minimal implementation without complex health checks or dependencies
- Integration with ASP.NET Core health check middleware

### Out of Scope
- Advanced health checks (database connectivity, dependency validation)
- Detailed diagnostic information or metrics
- Health check UI or dashboards
- Changes to stdio host (health checks not applicable to stdio transport)

## Affected Capabilities
- **health-endpoint** (NEW): Provides HTTP health check endpoint for server monitoring

## Dependencies
- None (no blocking dependencies)

## Risks and Mitigation
- **Risk**: Health endpoint could be confused with existing status resource in MCP
  - **Mitigation**: Health endpoint is HTTP-specific for infrastructure monitoring, while status resource is MCP protocol-level information
- **Risk**: Additional endpoint increases attack surface
  - **Mitigation**: Health endpoint returns minimal static information with no sensitive data

## Alternatives Considered
1. **Custom minimal API endpoint**: Simpler but less extensible; ASP.NET Core health check middleware provides standardized format and extensibility
2. **Reuse MCP status resource**: Not accessible to standard HTTP monitoring tools that expect REST endpoints
3. **No health endpoint**: Current approach; makes production monitoring difficult

## Success Criteria
- HTTP GET to `/health` returns 200 OK
- Response includes JSON body with health status indicator
- Endpoint does not require authentication
- No impact on existing MCP functionality
- Server can be monitored by standard HTTP health check tools
