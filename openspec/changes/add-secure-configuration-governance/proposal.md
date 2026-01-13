# Change: Establish secure configuration and secrets governance

## Why
Current deployments rely solely on static appsettings files and ad-hoc environment overrides. Without a dedicated secrets abstraction, validation pipeline, or rotation process, API keys and sensitive settings are easily misconfigured or leaked, blocking production readiness.

## What Changes
- Introduce a secrets provider abstraction supporting environment variables, development secrets, and external vault integrations.
- Add startup validation that fails fast on missing or invalid authentication configuration.
- Provide rotation guidance and helpers so operators can swap API keys without downtime.
- Document configuration layering, precedence, and secure storage recommendations.

## Impact
- Affected specs: configuration-security (new)
- Affected code: src/McpServer.Template.Host.Http/Program.cs, appsettings*, Directory.Build.props (for packages), docs/
