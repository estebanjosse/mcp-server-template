using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public sealed class CalculatorService : ICalculatorService
{
    public Task<CalcResponse> DivideAsync(CalcRequest request, CancellationToken cancellationToken = default)
    {
        if (request.B == 0)
        {
            throw new InvalidOperationException("Division by zero is not allowed");
        }

        var result = request.A / request.B;
        return Task.FromResult(new CalcResponse(result));
    }
}
