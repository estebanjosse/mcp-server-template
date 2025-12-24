using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Services;

public interface ICalculatorService
{
    Task<CalcResponse> DivideAsync(CalcRequest request, CancellationToken cancellationToken = default);
}
