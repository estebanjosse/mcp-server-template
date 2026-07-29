## 1. SDK and MCP registration

- [ ] 1.1 Update the centralized ModelContextProtocol package version to 2.0.0 and restore the solution successfully.
- [ ] 1.2 Refactor shared MCP capability registration to return one MCP server builder with tools, prompts, and resources registered once.
- [ ] 1.3 Attach the stdio transport to the shared MCP server builder and verify the stdio host builds and starts.

## 2. Stateless HTTP transport

- [ ] 2.1 Attach HTTP transport to the shared MCP server builder with stateless Streamable HTTP explicitly enabled at `/mcp`.
- [ ] 2.2 Remove default legacy SSE and stateful-session assumptions from HTTP host configuration.
- [ ] 2.3 Update HTTP integration tests to verify a valid sessionless MCP request is processed and an invalid protocol-version header returns HTTP 400.

## 3. Request-flow observability

- [ ] 3.1 Replace session increment/decrement instrumentation with request-in-flight instrumentation and rename the Prometheus gauge to `mcp_requests_in_flight`.
- [ ] 3.2 Update metrics tests to assert the new request-concurrency gauge and absence of `mcp_sessions_active`.

## 4. Documentation and verification

- [ ] 4.1 Update README and HTTP operations/development documentation for the stateless Streamable HTTP endpoint and breaking migration from stateful/SSE clients.
- [ ] 4.2 Run restore, build, targeted MCP and HTTP host tests, and the full test suite.
- [ ] 4.3 Pack, install, and generate HTTP-only and stdio-only template projects; build each generated project against SDK v2.
