using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public interface IEchoService
{
    Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken cancellationToken = default);
}
