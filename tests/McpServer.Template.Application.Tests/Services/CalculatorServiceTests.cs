using McpServer.Template.Application.Services;
using McpServer.Template.Contracts.DTOs;

namespace McpServer.Template.Application.Tests.Services;

public sealed class CalculatorServiceTests
{
    private readonly CalculatorService _sut = new();

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(100, 4, 25)]
    [InlineData(7, 2, 3.5)]
    [InlineData(-10, 2, -5)]
    [InlineData(10, -2, -5)]
    public async Task DivideAsync_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new CalcRequest(a, b);

        // Act
        var result = await _sut.DivideAsync(request);

        // Assert
        result.Result.Should().Be(expected);
    }

    [Fact]
    public async Task DivideAsync_WhenDivisorIsZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new CalcRequest(10, 0);

        // Act
        var act = async () => await _sut.DivideAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Division by zero is not allowed");
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(1, 1, 1)]
    public async Task DivideAsync_EdgeCases_ShouldReturnCorrectResult(double a, double b, double expected)
    {
        // Arrange
        var request = new CalcRequest(a, b);

        // Act
        var result = await _sut.DivideAsync(request);

        // Assert
        result.Result.Should().Be(expected);
    }
}
