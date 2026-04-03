using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Ports;

public interface IRuntimeToolsService
{
    Task<GetCurrentDateTimeResponse> GetCurrentDateTimeAsync(
        GetCurrentDateTimeRequest request,
        CancellationToken cancellationToken = default);

    Task<HttpFetchResponse> HttpFetchAsync(
        HttpFetchRequest request,
        CancellationToken cancellationToken = default);

    Task<HealthCheckResponse> HealthCheckAsync(
        HealthCheckRequest request,
        CancellationToken cancellationToken = default);
}