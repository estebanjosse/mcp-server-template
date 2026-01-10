## 1. Implementation
- [ ] 1.1 Add configuration binding for `Authentication:ApiKey` and optional header overrides, including environment variable support.
- [ ] 1.2 Introduce middleware that enforces the shared-secret guard on `/mcp` when enabled and bypasses other endpoints.
- [ ] 1.3 Add automated tests covering enabled/disabled guard behaviour, header overrides, and monitoring endpoint access.
- [ ] 1.4 Document the configuration keys in repository docs or README, highlighting usage and default behaviour.
