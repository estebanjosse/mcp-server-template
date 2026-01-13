# Change: Harden HTTP perimeter for production deployments

## Why
The HTTP host currently exposes `/mcp` without mandatory transport security or request filtering, preventing safe internet-facing deployments. Production consumers need TLS guidance, rate limiting, and secure defaults (headers, CORS) baked into the template rather than bolted on manually.

## What Changes
- Add first-class TLS support for Kestrel and reverse-proxy scenarios with documented certificate configuration.
- Introduce configurable rate limiting and request throttling to protect `/mcp` from abuse.
- Apply hardened security headers (HSTS, frame/Content Security Policy, X-Content-Type-Options) and safe CORS defaults.
- Document deployment guidance covering proxy termination, certificate management, and recommended limits.

## Impact
- Affected specs: http-security-hardening (new)
- Affected code: src/McpServer.Template.Host.Http/Program.cs, appsettings*, docs/
