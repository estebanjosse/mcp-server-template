# Implementation Tasks: Add Health Endpoint

**Change ID**: `add-health-endpoint`

## Tasks

- [ ] Register health check services in `src/McpServer.Template.Host.Http/Program.cs` using `builder.Services.AddHealthChecks()`
- [ ] Map health check endpoint in `src/McpServer.Template.Host.Http/Program.cs` using `app.MapHealthChecks("/health")`
- [ ] Configure health check response format to return JSON with proper content type
- [ ] Test health endpoint manually by running HTTP host and sending GET request to `/health`
- [ ] Verify endpoint returns expected JSON structure with "Healthy" status
- [ ] Verify existing MCP endpoint at `/mcp` continues to work correctly
- [ ] Update README.md to document the new `/health` endpoint, ASP.NET Core health check middleware usage, and its purpose

## Validation

Each task should be verified by:
- **Compilation**: Code builds without errors or warnings
- **Manual testing**: Start HTTP host (`dotnet run --project src/McpServer.Template.Host.Http`) and test endpoint
- **Functional verification**: Endpoint responds correctly to GET requests
- **Regression check**: Existing MCP functionality unaffected

## Order of Execution

Tasks should be completed sequentially in the order listed. Each task must be completed and verified before proceeding to the next one.
