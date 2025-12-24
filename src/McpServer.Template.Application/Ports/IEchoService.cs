using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Ports;

public interface IEchoService
{
    Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken cancellationToken = default);
}
