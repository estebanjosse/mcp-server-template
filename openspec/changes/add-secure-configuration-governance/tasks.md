## 1. Secrets Abstraction
- [ ] 1.1 Define an interface and default implementations for retrieving secrets from environment variables, user secrets, and pluggable external vaults.
- [ ] 1.2 Wire the API key guard to consume the abstraction and support runtime refresh.

## 2. Configuration Validation
- [ ] 2.1 Implement startup validation that ensures required configuration sections are present and well-formed.
- [ ] 2.2 Add diagnostics to surface configuration errors with actionable messages while avoiding secret disclosure.

## 3. Rotation & Documentation
- [ ] 3.1 Provide a rotation helper or documented workflow enabling key rollovers without downtime.
- [ ] 3.2 Update documentation describing configuration precedence, secret storage recommendations, and validation failure recovery.
