namespace McpServer.Template.Contracts.DTOs;

public sealed record ValidateJsonSchemaRequest(string Json, string Schema);

public sealed record ValidateJsonSchemaResponse(bool Valid, IReadOnlyList<JsonValidationError> Errors);

public sealed record JsonValidationError(string Path, string Message, string? Keyword = null);

public sealed record TransformJsonRequest(
    string Json,
    string Operation,
    IReadOnlyDictionary<string, string> FieldMap);

public sealed record TransformJsonResponse(string Operation, string ResultJson);