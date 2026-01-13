## ADDED Requirements
### Requirement: TLS Configuration Support
The HTTP host SHALL provide first-class TLS configuration for both direct Kestrel termination and reverse-proxy offload scenarios.

#### Scenario: Enable HTTPS via Kestrel certificate
- **GIVEN** an operator configures `Kestrel:Endpoints:Https` with a certificate path and password
- **WHEN** the host starts
- **THEN** the server binds HTTPS on the configured port without additional code changes
- **AND** `/mcp` is served exclusively over HTTPS when TLS is enabled

#### Scenario: Honor reverse proxy headers
- **GIVEN** the server runs behind a reverse proxy that forwards `X-Forwarded-Proto`
- **WHEN** TLS is terminated upstream
- **THEN** generated links and redirects respect the original HTTPS scheme
- **AND** HSTS headers remain accurate for the public endpoint

### Requirement: Rate Limiting Controls
The HTTP host SHALL expose configurable rate limiting to protect `/mcp` requests from abusive clients.

#### Scenario: Default limit protects `/mcp`
- **GIVEN** rate limiting is enabled with the default configuration
- **WHEN** a client exceeds the allowed request burst to `/mcp`
- **THEN** the server responds with HTTP 429 Too Many Requests
- **AND** the response includes a `Retry-After` header indicating when to retry

#### Scenario: Custom policy per client identity
- **GIVEN** an operator configures a named rate limiting policy using client IP or API key identity
- **WHEN** traffic from multiple clients arrives simultaneously
- **THEN** each client is throttled independently according to the configured limits

### Requirement: Secure Response Headers
The HTTP host SHALL emit hardened security headers by default with documented overrides.

#### Scenario: HSTS enabled for HTTPS traffic
- **GIVEN** the server is running with TLS enabled
- **WHEN** a client fetches any HTTP response
- **THEN** the response includes `Strict-Transport-Security: max-age=31536000; includeSubDomains`
- **AND** the header value is configurable without code changes

#### Scenario: Baseline browser hardening headers present
- **GIVEN** the default middleware configuration
- **WHEN** `/mcp` responds to a request
- **THEN** the response includes `X-Content-Type-Options: nosniff` and `X-Frame-Options: DENY`
- **AND** an optional Content-Security-Policy is emitted and documented

### Requirement: Restrictive CORS Policy
The HTTP host SHALL default to a deny-by-default CORS policy with explicit allow lists for origins that require browser access.

#### Scenario: Deny cross-origin requests by default
- **GIVEN** no origins are configured
- **WHEN** a browser sends a cross-origin `OPTIONS` request to `/mcp`
- **THEN** the server responds without `Access-Control-Allow-Origin`
- **AND** the request is rejected per CORS rules

#### Scenario: Allow configured origins
- **GIVEN** the operator lists specific origins in configuration
- **WHEN** a request originates from an allowed origin
- **THEN** the response includes `Access-Control-Allow-Origin` with that origin value
- **AND** the server allows credentials only when explicitly opted in

### Requirement: Perimeter Hardening Guidance
The project documentation SHALL provide an operational hardening checklist covering TLS, proxy deployment, and configuration of rate limiting and headers.

#### Scenario: TLS and proxy checklist published
- **GIVEN** an operator consults the documentation
- **WHEN** configuring TLS termination or reverse proxy headers
- **THEN** the guide lists required configuration keys and certificate management steps
- **AND** it explains verification commands to confirm the secure setup
