using System.Text.Json;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests;

public sealed class WireFormatTests
{
    [Theory]
    [InlineData("info: host started")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{},\"error\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":true,\"result\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{},\"id\":1}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/test\",\"params\":null}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/test\",\"params\":[]}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/test\",\"params\":false}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"unexpected/request\"}")]
    public void DecodeFrame_RejectsNoiseAndInvalidEnvelopes(string line)
        => Assert.ThrowsAny<Exception>(() => StdioSession.DecodeFrame(line));

    [Theory]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"id\":1,\"error\":{\"code\":-32603,\"message\":\"failure\"}}")]
    [InlineData("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/test\",\"params\":{}}")]
    public void DecodeFrame_AcceptsValidResponsesAndNotifications(string line)
        => Assert.Equal(JsonValueKind.Object, StdioSession.DecodeFrame(line).ValueKind);
}
