using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public sealed class EchoService : IEchoService
{
    public Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken cancellationToken = default)
    {
        var response = new EchoResponse(request.Message, DateTime.UtcNow);
        return Task.FromResult(response);
    }
}
