using McpServer.Template.Application.Ports;
using McpServer.Template.Contracts.DTOs;
using McpServer.Template.Mcp.Instrumentation;
using McpServer.Template.Mcp.Tools;

namespace McpServer.Template.Mcp.Tests.Tools;

public sealed class EchoToolTests
{
    private readonly IEchoService _echoService;
    private readonly IMcpMetricsRecorder _metricsRecorder;
    private readonly EchoTool _sut;

    public EchoToolTests()
    {
        _echoService = Substitute.For<IEchoService>();
        _metricsRecorder = Substitute.For<IMcpMetricsRecorder>();
        _sut = new EchoTool(_echoService, _metricsRecorder);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallEchoServiceWithCorrectRequest()
    {
        // Arrange
        const string message = "Test message";
        var expectedResponse = new EchoResponse(message, DateTime.UtcNow);
        _echoService.EchoAsync(Arg.Any<EchoRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExecuteAsync(message);

        // Assert
        await _echoService.Received(1).EchoAsync(
            Arg.Is<EchoRequest>(r => r.Message == message),
            Arg.Any<CancellationToken>());
        result.Should().Be(expectedResponse);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple message")]
    [InlineData("Message with special chars: @#$%")]
    public async Task ExecuteAsync_ShouldHandleVariousMessages(string message)
    {
        // Arrange
        var expectedResponse = new EchoResponse(message, DateTime.UtcNow);
        _echoService.EchoAsync(Arg.Any<EchoRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _sut.ExecuteAsync(message);

        // Assert
        result.Message.Should().Be(message);
    }
}
