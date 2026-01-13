## 1. Endpoints & Shutdown
- [ ] 1.1 Add a readiness endpoint with configurable health checks distinct from `/health`.
- [ ] 1.2 Implement graceful shutdown handling that drains in-flight requests before process exit.

## 2. Timeouts & Back-pressure
- [ ] 2.1 Configure request and response timeouts for `/mcp` operations and document defaults.
- [ ] 2.2 Introduce back-pressure controls limiting concurrent sessions or streaming requests.

## 3. SSE Keep-alives & Documentation
- [ ] 3.1 Emit periodic SSE keep-alive messages to maintain long-lived connections.
- [ ] 3.2 Publish operational guidance for deployment drains, timeout tuning, and incident response.
