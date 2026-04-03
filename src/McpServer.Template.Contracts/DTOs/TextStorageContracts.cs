namespace McpServer.Template.Contracts.DTOs;

public sealed record SaveTextRequest(string DocumentId, string Content, bool Overwrite = false);

public sealed record SaveTextResponse(
    string DocumentId,
    int SizeBytes,
    DateTimeOffset LastUpdatedUtc,
    bool Overwritten);

public sealed record ReadTextRequest(string DocumentId);

public sealed record ReadTextResponse(
    string DocumentId,
    string Content,
    int SizeBytes,
    DateTimeOffset LastUpdatedUtc);

public sealed record SearchTextRequest(string Query, int Limit = 10, bool CaseSensitive = false);

public sealed record SearchTextResponse(
    string Query,
    bool CaseSensitive,
    IReadOnlyList<SearchTextMatch> Matches);

public sealed record SearchTextMatch(string DocumentId, string Snippet, int OccurrenceCount);