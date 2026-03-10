# Configuration

This document is the single reference for all configurable options in the MCP server template. Options can be set via `appsettings.json` files and/or environment variables.

## How Configuration Works

ASP.NET Core loads configuration from multiple sources, in this order (last wins):

1. `appsettings.json` — base defaults
2. `appsettings.{Environment}.json` — environment-specific overrides (e.g. `appsettings.Development.json`)
3. Environment variables — highest precedence
4. Command-line arguments

The active environment is controlled by `ASPNETCORE_ENVIRONMENT` (HTTP host) or `DOTNET_ENVIRONMENT` (stdio host). When set to `Development`, the matching `appsettings.Development.json` is loaded on top of `appsettings.json`.

### Environment Variable Naming

ASP.NET Core maps environment variables to configuration keys using `__` (double underscore) as a section separator:

```text
Authentication:Mode       → Authentication__Mode
Authentication:ApiKeys:0  → Authentication__ApiKeys__0
Logging:LogLevel:Default  → Logging__LogLevel__Default
```

In addition, some options have explicit shorthand environment variables (e.g. `MCP_AUTH_MODE`). These are documented per section below and always take precedence over the standard mapping.

---

## Configuration Reference

### Authentication

**Applies to:** HTTP host only. The `/health` and `/metrics` endpoints are never authenticated.

| Config Key | Env Variable | Type | Default | Description |
|---|---|---|---|---|
| `Authentication:Mode` | `MCP_AUTH_MODE` | `none` \| `simple` \| `secure` | `simple` | Authentication strategy. |
| `Authentication:ApiKeys` | `MCP_AUTH_API_KEY` | string[] | — | API keys accepted in `simple` mode. Min 32 chars each. |
| `Authentication:HeaderName` | `MCP_AUTH_HEADER` | string? | `null` | Custom header for key extraction. `null` = `Authorization: Bearer`. |

**Mode details:**

| Mode | Behavior |
|---|---|
| `none` | All requests are allowed without credentials. |
| `simple` | Requests must present a valid API key. Returns `401` with `WWW-Authenticate: Bearer realm="MCP"` on failure. |
| `secure` | Reserved for future mTLS/OAuth. Returns `501 Not Implemented`. |

**Validation rules (enforced at startup):**

- Mode must be `none`, `simple`, or `secure` (case-insensitive). Invalid values cause a startup failure.
- In `simple` mode, at least one key is required, and every key must be ≥ 32 characters.
- `HeaderName`, if set, must not contain spaces, colons, or control characters.

**Environment variable behavior:**

- `MCP_AUTH_API_KEY` sets a **single** key, replacing the entire `ApiKeys` array. For multiple keys via env vars, use the standard ASP.NET mapping: `Authentication__ApiKeys__0`, `Authentication__ApiKeys__1`, etc.
- `MCP_AUTH_MODE` overrides the mode regardless of what `appsettings.json` says.

**Examples:**

```json
// appsettings.json — simple mode with two keys (for rotation)
{
  "Authentication": {
    "Mode": "simple",
    "ApiKeys": [
      "primary-key-at-least-32-characters-long",
      "secondary-key-at-least-32-characters-long"
    ]
  }
}
```

```json
// appsettings.json — custom header instead of Authorization: Bearer
{
  "Authentication": {
    "Mode": "simple",
    "ApiKeys": ["my-secret-key-at-least-32-characters-long"],
    "HeaderName": "X-Api-Key"
  }
}
```

```powershell
# Environment variables — single key, no appsettings needed
$env:MCP_AUTH_MODE = "simple"
$env:MCP_AUTH_API_KEY = "my-secret-api-key-at-least-32-characters-long"
```

```powershell
# Environment variables — multiple keys via standard ASP.NET mapping
$env:Authentication__Mode = "simple"
$env:Authentication__ApiKeys__0 = "primary-key-at-least-32-characters-long-1"
$env:Authentication__ApiKeys__1 = "secondary-key-at-least-32-characters-long"
```

---

### Metrics

**Applies to:** HTTP host only.

| Config Key | Env Variable | Type | Default (prod) | Default (dev) | Description |
|---|---|---|---|---|---|
| `Metrics:Enabled` | `MCP_METRICS_ENABLED` | bool | `false` | `true` | Enable Prometheus metrics at `/metrics`. |

**Examples:**

```json
{
  "Metrics": {
    "Enabled": true
  }
}
```

```powershell
$env:MCP_METRICS_ENABLED = "true"
```

---

### Logging

**Applies to:** Both HTTP and stdio hosts.

| Config Key | Env Variable | Type | Default (prod) | Default (dev) | Description |
|---|---|---|---|---|---|
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | LogLevel | `Information` | `Debug` | Default log level for all categories. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Logging__LogLevel__Microsoft.AspNetCore` | LogLevel | `Warning` | `Warning` | ASP.NET Core framework logs. |
| `Logging:LogLevel:ModelContextProtocol` | `Logging__LogLevel__ModelContextProtocol` | LogLevel | `Debug` | `Trace` | MCP SDK protocol logs. |

Valid log levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`.

---

### MCP Server Metadata

**Applies to:** Both HTTP and stdio hosts.

| Config Key | Env Variable | Type | Default | Description |
|---|---|---|---|---|
| `McpServer:Name` | `McpServer__Name` | string | `McpServer.Template` | Server name reported in MCP responses. |
| `McpServer:Version` | `McpServer__Version` | string | `1.0.0` | Server version. |
| `McpServer:Description` | `McpServer__Description` | string | `A clean, scalable MCP server template with .NET 10` | Human-readable description. |

---

### ASP.NET Core / Host

These are standard .NET variables, not specific to this template, but important for deployment.

| Env Variable | Default | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` (launch) | Active environment for the HTTP host. Controls which `appsettings.{env}.json` is loaded. |
| `DOTNET_ENVIRONMENT` | `Development` (launch) | Equivalent for the stdio host (generic host). |
| `ASPNETCORE_URLS` | `http://+:5000` (Docker) | Listen addresses. Example: `http://+:8080` to change port. |
| `AllowedHosts` | `*` | Semicolon-separated list of allowed host headers. `*` allows all. |

---

## File Reference

| File | Scope | Purpose |
|---|---|---|
| `src/McpServer.Template.Host.Http/appsettings.json` | HTTP host | Production defaults |
| `src/McpServer.Template.Host.Http/appsettings.Development.json` | HTTP host | Development overrides (auth=none, metrics=on, verbose logs) |
| `src/McpServer.Template.Host.Stdio/appsettings.json` | Stdio host | Logging and server metadata only |
| `Dockerfile` | Docker | Sets `ASPNETCORE_URLS`, exposes port 5000, health check |

## Environment Differences

| Setting | Production (`appsettings.json`) | Development (`appsettings.Development.json`) |
|---|---|---|
| Authentication mode | `simple` | `none` |
| Metrics | Disabled | Enabled |
| Default log level | `Information` | `Debug` |
| MCP SDK log level | `Debug` | `Trace` |
