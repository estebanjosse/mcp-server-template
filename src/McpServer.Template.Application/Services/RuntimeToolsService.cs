using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public sealed class RuntimeToolsService : IRuntimeToolsService
{
    public Task<GetCurrentDateTimeResponse> GetCurrentDateTimeAsync(
        GetCurrentDateTimeRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }

    public Task<HttpFetchResponse> HttpFetchAsync(
        HttpFetchRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }

    public Task<HealthCheckResponse> HealthCheckAsync(
        HealthCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Implemented in a later OpenSpec task.");
    }
}