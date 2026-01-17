using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;
using McpServer.Template.Mcp.Instrumentation;
using McpServer.Template.Mcp.Tools;
using ModelContextProtocol;

namespace McpServer.Template.Mcp.Tests.Tools;

public sealed class CalcDivideToolTests
{
    private readonly ICalculatorService _calculatorService;
    private readonly IMcpMetricsRecorder _metricsRecorder;
    private readonly CalcDivideTool _sut;

    public CalcDivideToolTests()
    {
        _calculatorService = Substitute.For<ICalculatorService>();
        _metricsRecorder = Substitute.For<IMcpMetricsRecorder>();
        _sut = new CalcDivideTool(_calculatorService, _metricsRecorder);
    }

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(100, 4, 25)]
    [InlineData(7, 2, 3.5)]
    public async Task ExecuteAsync_WithValidInputs_ShouldReturnResult(double a, double b, double expected)
    {
        // Arrange
        var expectedResponse = new CalcResponse(expected);
        _calculatorService.DivideAsync(Arg.Any<CalcRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExecuteAsync(a, b);

        // Assert
        await _calculatorService.Received(1).DivideAsync(
            Arg.Is<CalcRequest>(r => r.A == a && r.B == b),
            Arg.Any<CancellationToken>());
        result.Result.Should().Be(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDivisionByZero_ShouldThrowMcpException()
    {
        // Arrange
        _calculatorService
            .DivideAsync(Arg.Is<CalcRequest>(r => r.B == 0), Arg.Any<CancellationToken>())
            .Returns<CalcResponse>(_ => throw new InvalidOperationException("Division by zero is not allowed"));

        // Act
        var act = async () => await _sut.ExecuteAsync(10, 0);

        // Assert
        await act.Should().ThrowAsync<McpException>()
            .Where(e => e.Message.Contains("Division by zero"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var expectedResponse = new CalcResponse(5);
        _calculatorService.DivideAsync(Arg.Any<CalcRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _sut.ExecuteAsync(10, 2, cts.Token);

        // Assert
        await _calculatorService.Received(1).DivideAsync(
            Arg.Any<CalcRequest>(),
            cts.Token);
    }
}
