using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public sealed class JsonProcessingService : IJsonProcessingService
{
    public Task<ValidateJsonSchemaResponse> ValidateJsonSchemaAsync(
        ValidateJsonSchemaRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }

    public Task<TransformJsonResponse> TransformJsonAsync(
        TransformJsonRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }
}