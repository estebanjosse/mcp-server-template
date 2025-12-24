using System.ComponentModel;
using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace McpServer.Template.Mcp.Tools;

[McpServerToolType]
public sealed class CalcDivideTool(ICalculatorService calculatorService)
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
            return await calculatorService.DivideAsync(request, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}
