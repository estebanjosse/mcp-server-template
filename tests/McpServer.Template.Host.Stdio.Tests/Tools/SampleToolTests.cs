using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests.Tools;

public sealed class SampleToolTests : StdioHostTestBase
{
    [Theory]
    [InlineData("echo", new[] { "message" }, new[] { "string" })]
    [InlineData("calc_divide", new[] { "a", "b" }, new[] { "number", "number" })]
    public async Task ToolDiscovery_ExposesRequiredInputSchema(string name, string[] parameters, string[] types)
    {
        Host.Initialization.GetProperty("capabilities").GetProperty("tools").ValueKind.Should().Be(JsonValueKind.Object);
        var tools = (await Host.RequestAsync("tools/list")).GetProperty("tools").EnumerateArray().ToArray();
        var schema = tools.Single(t => t.GetProperty("name").GetString() == name).GetProperty("inputSchema");
        schema.GetProperty("type").GetString().Should().Be("object");
        schema.GetProperty("required").EnumerateArray().Select(p => p.GetString()).Should().BeEquivalentTo(parameters);
        schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(parameters);
        for (var index = 0; index < parameters.Length; index++)
            schema.GetProperty("properties").GetProperty(parameters[index]).GetProperty("type").GetString().Should().Be(types[index]);
    }

    [Fact]
    public async Task Echo_ActivatesRealToolAndReturnsExactUnicodeDtoWithUtcTimestamp()
    {
        const string message = "stdio E2E: quote \" slash \\ Unicode café ☃ 🚀\nsecond line";
        var before = DateTimeOffset.UtcNow;
        var dto = SuccessfulDto(await Host.RequestAsync("tools/call", new { name = "echo", arguments = new { message } }));
        var after = DateTimeOffset.UtcNow;
        dto.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("message", "timestamp");
        dto.GetProperty("message").GetString().Should().Be(message);
        var stamp = dto.GetProperty("timestamp").GetString()!;
        stamp.Should().MatchRegex(@"(Z|\+00:00)$");
        DateTimeOffset.Parse(stamp, CultureInfo.InvariantCulture).Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task Divide_ActivatesRealToolAndReturnsExactlyThreePointFive()
    {
        var dto = SuccessfulDto(await Host.RequestAsync("tools/call", new { name = "calc_divide", arguments = new { a = 7, b = 2 } }));
        dto.EnumerateObject().Select(p => p.Name).Should().Equal("result");
        dto.GetProperty("result").GetDouble().Should().Be(3.5);
    }

    [Fact]
    public async Task DivideByZero_ReturnsExactBusinessErrorNotActivationFailure()
        => AssertDivisionByZero(await Host.RequestAsync("tools/call", new { name = "calc_divide", arguments = new { a = 7, b = 0 } }));

    internal static JsonElement SuccessfulDto(JsonElement result)
    {
        if (result.TryGetProperty("isError", out var isError))
            isError.ValueKind.Should().Be(JsonValueKind.False, $"a successful tool must not return an activation/business error: {result}");
        var block = result.GetProperty("content").EnumerateArray().Should().ContainSingle().Subject;
        block.GetProperty("type").GetString().Should().Be("text");
        using var document = JsonDocument.Parse(block.GetProperty("text").GetString()!);
        var dto = document.RootElement;
        dto.ValueKind.Should().Be(JsonValueKind.Object);
        if (result.TryGetProperty("structuredContent", out var structured))
            JsonElement.DeepEquals(structured, dto).Should().BeTrue("structured and text content must describe the same DTO");
        return dto.Clone();
    }

    internal static void AssertDivisionByZero(JsonElement result)
    {
        result.GetProperty("isError").ValueKind.Should().Be(JsonValueKind.True);
        var block = result.GetProperty("content").EnumerateArray().Should().ContainSingle().Subject;
        block.GetProperty("type").GetString().Should().Be("text");
        block.GetProperty("text").GetString().Should().BeOneOf(
            "Division by zero is not allowed",
            "An error occurred invoking 'calc_divide': Division by zero is not allowed");
    }
}
