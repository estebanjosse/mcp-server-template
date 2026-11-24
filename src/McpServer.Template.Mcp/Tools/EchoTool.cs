using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;
using McpServer.Template.Mcp.Instrumentation;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Template.Mcp.Tools;

[McpServerToolType]
public sealed class EchoTool(IEchoService echoService, IMcpMetricsRecorder metricsRecorder)
{
    [McpServerTool(Name = "echo")]
    [Description("Echoes back the provided message with a timestamp")]
    public async Task<EchoResponse> ExecuteAsync(
        [Description("The message to echo back")] string message,
        CancellationToken cancellationToken = default)
    {
        var request = new EchoRequest(message);
        try
        {
            return await echoService.EchoAsync(request, cancellationToken);
        }
        finally
        {
            metricsRecorder.RecordToolInvocation("echo");
        }
    }
}
