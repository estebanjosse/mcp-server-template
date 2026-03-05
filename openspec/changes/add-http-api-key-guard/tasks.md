## 1. Foundation — Mode switching and strategy pattern
- [x] 1.1 Create `AuthenticationOptions` with `Mode` (enum: None, Simple, Secure), `ApiKeys` (string[]), `HeaderName` (string?), and env var overrides (`MCP_AUTH_MODE`, `MCP_AUTH_API_KEY`, `MCP_AUTH_HEADER`).
- [x] 1.2 Add startup validation: reject invalid mode values, enforce minimum key length (32) in simple mode, discard empty keys, reject invalid header names.
- [ ] 1.3 Define `IMcpAuthStrategy` interface (in Host.Http) with `AuthenticateAsync(HttpContext)` → `AuthResult` and register strategy dispatch middleware scoped to `/mcp` only.
- [ ] 1.4 Implement `NoneAuthStrategy` (pass-through) and `SecurePlaceholderStrategy` (returns 501 with descriptive message).
- [ ] 1.5 Add tests: mode selection from config and env var, invalid mode rejection, env var override precedence, 501 for unimplemented secure mode, monitoring endpoints bypass.

## 2. Simple mode — API key guard
- [ ] 2.1 Implement `ApiKeyAuthStrategy` with constant-time comparison (`CryptographicOperations.FixedTimeEquals`) across all configured keys without short-circuiting.
- [ ] 2.2 Support `Authorization: Bearer <key>` by default and custom header via `Authentication:HeaderName`.
- [ ] 2.3 Support dual-key rotation: accept any key in `Authentication:ApiKeys` array; coerce single string value to array.
- [ ] 2.4 Return `WWW-Authenticate: Bearer realm="MCP"` on 401 responses.
- [ ] 2.5 Add tests: valid key accepted, invalid key rejected, dual-key rotation, custom header, constant-time comparison (no short-circuit), single-string coercion.

## 3. Security — Logging and brute-force protection
- [ ] 3.1 Emit structured auth event logs (Information on success, Warning on failure) with client IP, path, mode, result — never log credentials.
- [ ] 3.2 Implement per-IP brute-force mitigation: track consecutive failures, add `Retry-After` with progressive delay after 5 failures in 60s, reset on success.
- [ ] 3.3 Add tests: log entries contain expected fields, log entries never contain key material, brute-force delay triggers and resets correctly.

## 4. Configuration and documentation
- [ ] 4.1 Update `appsettings.json` with `Authentication` section (Mode, ApiKeys, HeaderName) with commented examples.
- [ ] 4.2 Document configuration keys, mode descriptions, rotation workflow, and env var overrides in docs/.
