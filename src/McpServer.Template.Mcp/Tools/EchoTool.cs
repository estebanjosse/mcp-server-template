using System.ComponentModel;
using McpServer.Template.Application.Services;
using McpServer.Template.Contracts.DTOs;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Tools;

[McpServerToolType]
public sealed class EchoTool(IEchoService echoService)
{
    [McpServerTool(Name = "echo")]
    [Description("Echoes back the provided message with a timestamp")]
    public async Task<EchoResponse> ExecuteAsync(
        [Description("The message to echo back")] string message,
        CancellationToken cancellationToken = default)
    {
        var request = new EchoRequest(message);
        return await echoService.EchoAsync(request, cancellationToken);
    }
}
