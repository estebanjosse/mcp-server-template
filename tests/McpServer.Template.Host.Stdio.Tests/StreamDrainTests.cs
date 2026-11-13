using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests;

public sealed class StreamDrainTests
{
    private const string Response = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{}}";

    [Theory]
    [InlineData("info: host started\n", "invalid start")]
    [InlineData(Response, "Unterminated stdout frame")]
    public async Task StdoutDrain_RetainsNoiseAndUnterminatedFramesAsViolations(string text, string expected)
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        var capture = new StringBuilder();
        List<string> violations = [];
        var frames = Channel.CreateUnbounded<JsonElement>();
        await StdioSession.DrainAsync(reader, capture, frames, violations, CancellationToken.None);
        Assert.Contains(expected, Assert.Single(violations));
        Assert.Equal(text, capture.ToString());
        Assert.False(frames.Reader.TryRead(out _));
        Assert.True(frames.Reader.Completion.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task StdoutDrain_EmitsCompleteFrameAndFinishesOnEof()
    {
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(Response + "\n")));
        var frames = Channel.CreateUnbounded<JsonElement>();
        List<string> violations = [];
        await StdioSession.DrainAsync(reader, new StringBuilder(), frames, violations, CancellationToken.None);
        Assert.Empty(violations);
        Assert.Equal(1, (await frames.Reader.ReadAsync()).GetProperty("id").GetInt32());
        await frames.Reader.Completion.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task StderrDrain_RetainsDiagnosticsWithoutParsingThemAsProtocol()
    {
        const string text = "info: host started\nretained diagnostic\n";
        using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        var capture = new StringBuilder();
        List<string> violations = [];
        await StdioSession.DrainAsync(reader, capture, null, violations, CancellationToken.None);
        Assert.Equal(text, capture.ToString());
        Assert.Empty(violations);
    }

    [Fact]
    public async Task StdoutDrain_PropagatesReaderFailureInsteadOfReportingSuccessfulDrain()
    {
        using var reader = new ControlledReader(fail: true);
        var task = StdioSession.DrainAsync(reader, new StringBuilder(), Channel.CreateUnbounded<JsonElement>(), [], CancellationToken.None);
        var exception = await Assert.ThrowsAsync<IOException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal("Controlled stream failure", exception.Message);
        Assert.True(task.IsFaulted);
    }

    [Fact]
    public async Task StdoutDrain_CanCancelBlockedPipeWithoutClaimingSuccessfulDrain()
    {
        using var reader = new ControlledReader(fail: false);
        using var cancellation = new CancellationTokenSource();
        var task = StdioSession.DrainAsync(reader, new StringBuilder(), Channel.CreateUnbounded<JsonElement>(), [], cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(task.IsCanceled);
    }

    private sealed class ControlledReader(bool fail) : StreamReader(Stream.Null)
    {
        public override async ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            if (fail) throw new IOException("Controlled stream failure");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }
    }
}
