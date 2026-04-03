namespace McpServer.Template.Contracts.DTOs;

public sealed record GetCurrentDateTimeRequest;

public sealed record GetCurrentDateTimeResponse(DateTimeOffset CurrentUtcDateTime, long UnixTimeSeconds);

public sealed record HttpFetchRequest(string Url);

public sealed record HttpFetchResponse(
    string Url,
    int StatusCode,
    string ContentType,
    string Content,
    bool Truncated);