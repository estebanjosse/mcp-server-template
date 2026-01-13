## ADDED Requirements
### Requirement: Secrets Provider Abstraction
The system SHALL expose a secrets provider abstraction that supports multiple backing stores without leaking sensitive values.

#### Scenario: Environment variable provider
- **GIVEN** the API key secret is present in `MCP_AUTH_API_KEY`
- **WHEN** the provider resolves secrets
- **THEN** the HTTP guard receives the secret without requiring direct environment access logic in application code

#### Scenario: External vault extension point
- **GIVEN** a custom provider is registered
- **WHEN** the application starts
- **THEN** the secrets abstraction retrieves values from the external vault implementation without code changes to API consumers

### Requirement: Configuration Validation Pipeline
The application SHALL validate required configuration at startup and fail fast when inputs are missing or invalid.

#### Scenario: Missing API key reported with guidance
- **GIVEN** the guard is enabled but no secret value is resolved
- **WHEN** the application starts
- **THEN** startup fails with a descriptive error message listing supported configuration sources
- **AND** the message redacts sensitive placeholders

#### Scenario: Invalid header name rejected
- **GIVEN** `Authentication:HeaderName` contains characters that are not allowed in HTTP headers
- **WHEN** configuration binding occurs
- **THEN** validation fails and the server refuses to start until the value is corrected

### Requirement: Runtime Secret Refresh Support
The system SHALL allow reloading authentication secrets without requiring application restarts when supported by the provider.

#### Scenario: Secret rotated at runtime
- **GIVEN** the secrets provider signals that the API key value changed
- **WHEN** the new value is retrieved
- **THEN** subsequent requests are validated against the new secret without restarting the process

### Requirement: Secure Configuration Documentation
Project documentation SHALL describe configuration precedence, secret storage guidance, and rotation workflows.

#### Scenario: Documented rotation workflow
- **GIVEN** an operator follows the configuration guide
- **WHEN** rotating the API key
- **THEN** the guide outlines steps to publish a new secret, trigger reload, and decommission the old value safely
- **AND** it references required tooling commands for each supported provider
