using System.Text.Json;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests.Tools;

public sealed class SampleToolAssertionTests
{
    [Theory]
    [InlineData("Unable to activate tool")]
    [InlineData("An error occurred invoking 'calc_divide'.")]
    [InlineData("An error occurred invoking 'calc_divide': Unable to activate tool")]
    [InlineData("An error occurred invoking 'other_tool': Division by zero is not allowed")]
    [InlineData("An error occurred invoking 'calc_divide': Division by zero is not allowed extra text")]
    public void DivisionByZeroAssertion_RejectsUnrelatedErrors(string message)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            isError = true, content = new[] { new { type = "text", text = message } }
        }));
        Assert.ThrowsAny<Exception>(() => SampleToolTests.AssertDivisionByZero(document.RootElement));
    }

    [Theory]
    [InlineData("Division by zero is not allowed")]
    [InlineData("An error occurred invoking 'calc_divide': Division by zero is not allowed")]
    public void DivisionByZeroAssertion_AcceptsOnlyExactBusinessMessages(string message)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            isError = true, content = new[] { new { type = "text", text = message } }
        }));
        SampleToolTests.AssertDivisionByZero(document.RootElement);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("0")]
    [InlineData("null")]
    [InlineData("\"false\"")]
    public void SuccessfulDtoAssertion_RejectsErrorsAndNonBooleanFlags(string flag)
    {
        using var document = JsonDocument.Parse("{\"isError\":" + flag + ",\"content\":[{\"type\":\"text\",\"text\":\"{\\\"result\\\":3.5}\"}]}");
        Assert.ThrowsAny<Exception>(() => SampleToolTests.SuccessfulDto(document.RootElement));
    }

    [Theory]
    [InlineData("{\"result\":3.5}")]
    [InlineData("{\"message\":\"café ☃\",\"timestamp\":\"2026-01-01T00:00:00Z\"}")]
    public void SuccessfulDtoAssertion_RejectsStructuredTextMismatch(string text)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            content = new[] { new { type = "text", text } }, structuredContent = new { unexpected = true }
        }));
        Assert.ThrowsAny<Exception>(() => SampleToolTests.SuccessfulDto(document.RootElement));
    }
}
