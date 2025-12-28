# health-endpoint Specification

## Purpose
TBD - created by archiving change add-health-endpoint. Update Purpose after archive.
## Requirements
### Requirement: Health Endpoint Availability
The HTTP host MUST expose a `/health` endpoint that responds to HTTP GET requests without requiring authentication.

#### Scenario: Basic health check request
**WHEN** a client sends `GET /health`  
**THEN** the server responds with HTTP status 200 OK  
**AND** the response includes a JSON body

#### Scenario: Health check without authentication
**WHEN** a client sends `GET /health` without any authentication headers  
**THEN** the server responds successfully with HTTP status 200 OK

### Requirement: Health Response Format
The health endpoint MUST return a JSON response with a `status` field indicating server health.

#### Scenario: Successful health response structure
**WHEN** a client sends `GET /health`  
**THEN** the response body contains a JSON object  
**AND** the JSON object includes a `status` field with value `"healthy"`  
**AND** the response Content-Type header is `application/json`

### Requirement: Health Endpoint Independence
The health endpoint MUST operate independently from MCP protocol functionality and not interfere with existing MCP endpoints.

#### Scenario: MCP endpoint unaffected by health endpoint
**GIVEN** the HTTP server is running with both `/health` and `/mcp` endpoints  
**WHEN** a client accesses the `/health` endpoint  
**THEN** the `/mcp` endpoint continues to function normally for MCP protocol requests

#### Scenario: Health check works when MCP is idle
**GIVEN** no active MCP connections exist  
**WHEN** a client sends `GET /health`  
**THEN** the server responds with HTTP status 200 OK  
**AND** the health status indicates the server is healthy

### Requirement: Minimal Performance Impact
The health endpoint MUST respond quickly with minimal resource consumption to support frequent polling by monitoring systems.

#### Scenario: Fast health check response
**WHEN** a monitoring system polls `GET /health` every 5 seconds  
**THEN** each request completes in less than 100ms  
**AND** the server remains responsive to other requests

### Requirement: ASP.NET Core Health Check Middleware Integration
The HTTP host MUST use ASP.NET Core health check middleware to implement the health endpoint functionality.

#### Scenario: Health checks service registration
**WHEN** the application starts  
**THEN** health check services are registered in the dependency injection container  
**AND** the health check middleware is configured in the request pipeline

#### Scenario: Health check endpoint mapping
**WHEN** the application maps endpoints  
**THEN** the `/health` endpoint is mapped using `MapHealthChecks`  
**AND** the endpoint responds to health check requests

#### Scenario: Healthy status response
**WHEN** all registered health checks pass  
**THEN** the endpoint returns HTTP 200 OK  
**AND** the response body indicates "Healthy" status

---

