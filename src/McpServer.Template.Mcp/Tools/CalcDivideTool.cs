using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;
using McpServer.Template.Mcp.Instrumentation;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpServer.Template.Mcp.Tools;

[McpServerToolType]
public sealed class CalcDivideTool(ICalculatorService calculatorService, IMcpMetricsRecorder metricsRecorder)
{
    [McpServerTool(Name = "calc_divide")]
    [Description("Divides two numbers and returns the result")]
    public async Task<CalcResponse> ExecuteAsync(
        [Description("The dividend (numerator)")] double a,
        [Description("The divisor (denominator)")] double b,
        CancellationToken cancellationToken = default)
    {
        var request = new CalcRequest(a, b);
        
        try
        {
            var result = await calculatorService.DivideAsync(request, cancellationToken);
            metricsRecorder.RecordToolInvocation("calc_divide");
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
