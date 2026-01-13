## 1. TLS Enablement
- [ ] 1.1 Add TLS configuration options for Kestrel and document reverse-proxy termination patterns.
- [ ] 1.2 Provide scripts or guidance to provision and load certificates locally for testing.

## 2. Perimeter Protections
- [ ] 2.1 Implement configurable rate limiting middleware with sensible defaults for `/mcp` requests.
- [ ] 2.2 Apply security header middleware (HSTS, X-Content-Type-Options, X-Frame-Options/CSP) and ensure they are configurable but enabled by default.
- [ ] 2.3 Define restrictive default CORS policy allowing explicit opt-in origins.

## 3. Validation & Docs
- [ ] 3.1 Add automated coverage verifying TLS endpoints, rate limiting responses, and headers.
- [ ] 3.2 Update documentation with hardening guidance and operational checklist.
