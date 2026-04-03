namespace McpServer.Template.Contracts.DTOs;

public sealed record HealthCheckRequest;

public sealed record HealthCheckResponse(
    string Status,
    string Version,
    TimeSpan Uptime,
    DateTimeOffset CurrentUtcDateTime,
    HealthCheckStorageDiagnostics Storage,
    HealthCheckHttpFetchDiagnostics HttpFetch);

public sealed record HealthCheckStorageDiagnostics(bool Available, string? StoragePath);

public sealed record HealthCheckHttpFetchDiagnostics(bool Enabled, string PolicySummary);