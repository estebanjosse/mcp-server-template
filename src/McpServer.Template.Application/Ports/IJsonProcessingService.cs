using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Ports;

public interface IJsonProcessingService
{
    Task<ValidateJsonSchemaResponse> ValidateJsonSchemaAsync(
        ValidateJsonSchemaRequest request,
        CancellationToken cancellationToken = default);

    Task<TransformJsonResponse> TransformJsonAsync(
        TransformJsonRequest request,
        CancellationToken cancellationToken = default);
}