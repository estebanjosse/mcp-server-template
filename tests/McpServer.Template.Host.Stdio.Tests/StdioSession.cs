using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FluentAssertions;

namespace McpServer.Template.Host.Stdio.Tests;

/// <summary>
/// Strict wire client: invalid output is retained, never silently treated as protocol traffic.
/// Every test owns one real host; no application services or logging settings are replaced.
/// </summary>
internal sealed class StdioSession : IAsyncDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);
    private readonly Process process;
    private readonly CancellationTokenSource readersCancellation = new();
    private readonly Channel<JsonElement> frames = Channel.CreateUnbounded<JsonElement>();
    private readonly List<string> violations = [];
    private readonly HashSet<int> responses = [];
    private readonly StringBuilder stdout = new(), stderr = new(), client = new();
    private readonly Task stdoutDrain, stderrDrain;
    private readonly string artifacts;
    private int nextId;
    private bool stopped;
    private bool disposed;

    public JsonElement Initialization { get; private set; }
    public string Stderr { get { lock (stderr) return stderr.ToString(); } }

    public StdioSession(string testName)
    {
        artifacts = Path.Combine(Environment.GetEnvironmentVariable("MCP_TEST_ARTIFACTS") ?? Path.GetTempPath(),
            $"stdio-{testName}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(artifacts);
        var dll = Environment.GetEnvironmentVariable("MCP_STDIO_HOST_DLL")
            ?? Path.Combine(AppContext.BaseDirectory, "stdio", "McpServer.Template.Host.Stdio.dll");
        dll = Path.GetFullPath(dll);
        if (!File.Exists(dll)) throw new FileNotFoundException("Build the stdio host before running its integration tests.", dll);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = Environment.GetEnvironmentVariable("MCP_STDIO_HOST_CWD") ?? Path.GetDirectoryName(dll)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false, true),
            StandardErrorEncoding = new UTF8Encoding(false, true),
            StandardInputEncoding = new UTF8Encoding(false)
        };
        start.ArgumentList.Add(dll);
        process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the stdio host.");
        stdoutDrain = DrainAsync(process.StandardOutput, stdout, frames, violations, readersCancellation.Token);
        stderrDrain = DrainAsync(process.StandardError, stderr, null, violations, readersCancellation.Token);
    }

    public async Task InitializeAsync()
    {
        Initialization = await RequestAsync("initialize", new
        {
            protocolVersion = "2025-11-25", capabilities = new { },
            clientInfo = new { name = "stdio-integration-tests", version = "1.0" }
        });
        using var timeout = new CancellationTokenSource(RequestTimeout);
        await SendAsync(new { jsonrpc = "2.0", method = "notifications/initialized" }, timeout.Token);
    }

    public async Task<JsonElement> RequestAsync(string method, object? parameters = null)
    {
        var id = ++nextId;
        using var timeout = new CancellationTokenSource(RequestTimeout);
        await SendAsync(new { jsonrpc = "2.0", id, method, @params = parameters ?? new { } }, timeout.Token);
        try
        {
            while (true)
            {
                var frame = await frames.Reader.ReadAsync(timeout.Token);
                if (frame.TryGetProperty("method", out _)) continue;
                var responseId = frame.GetProperty("id").GetInt32();
                responseId.Should().Be(id, "responses must correlate to the outstanding request");
                responses.Add(responseId).Should().BeTrue("duplicate response IDs are forbidden");
                frame.TryGetProperty("error", out var error).Should().BeFalse($"{method} must succeed: {error}");
                return frame.GetProperty("result");
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
            throw new InvalidOperationException($"No response to {method}. Evidence: {artifacts}\nStderr: {Stderr}", exception);
        }
    }

    private async Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        var line = JsonSerializer.Serialize(message);
        client.AppendLine(line);
        await process.StandardInput.WriteLineAsync(line.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    internal static async Task DrainAsync(StreamReader reader, StringBuilder capture, Channel<JsonElement>? frames,
        List<string> violations, CancellationToken cancellationToken)
    {
        var isStdout = frames is not null;
        var buffer = new char[4096];
        var pending = new StringBuilder();
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
            {
                lock (capture) capture.Append(buffer, 0, count);
                if (!isStdout) continue;
                foreach (var character in buffer.AsSpan(0, count))
                {
                    if (character != '\n') { pending.Append(character); continue; }
                    try { frames!.Writer.TryWrite(DecodeFrame(pending.ToString())); }
                    catch (Exception exception) when (exception is JsonException or InvalidDataException)
                    {
                        lock (violations) violations.Add(exception.Message + ": " + pending);
                    }
                    pending.Clear();
                }
            }
            if (isStdout && pending.Length > 0)
                lock (violations) violations.Add("Unterminated stdout frame: " + pending);
        }
        finally
        {
            if (isStdout) frames!.Writer.TryComplete();
        }
    }

    internal static JsonElement DecodeFrame(string line)
    {
        using var document = JsonDocument.Parse(line);
        var frame = document.RootElement;
        static void Require(bool valid)
        {
            if (!valid) throw new InvalidDataException("Expected a valid MCP JSON-RPC 2.0 frame");
        }
        Require(frame.ValueKind == JsonValueKind.Object);
        Require(frame.EnumerateObject().Select(p => p.Name).Distinct().Count() == frame.EnumerateObject().Count());
        Require(frame.TryGetProperty("jsonrpc", out var version) && version.ValueKind == JsonValueKind.String && version.GetString() == "2.0");
        var hasId = frame.TryGetProperty("id", out var id);
        var hasResult = frame.TryGetProperty("result", out _);
        var hasError = frame.TryGetProperty("error", out var error);
        if (frame.TryGetProperty("method", out var method))
        {
            // The client advertised no capabilities requiring server-to-client requests.
            Require(!hasId && method.ValueKind == JsonValueKind.String && !hasResult && !hasError);
            Require(!frame.TryGetProperty("params", out var parameters) || parameters.ValueKind == JsonValueKind.Object);
        }
        else
        {
            Require(hasId && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out _) && hasResult != hasError);
            if (hasError)
                Require(error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var code) && code.TryGetInt32(out _)
                    && error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String);
        }
        return frame.Clone();
    }

    public async Task StopAsync()
    {
        if (stopped) return;
        process.HasExited.Should().BeFalse("the host must remain alive until stdin EOF");
        process.StandardInput.Close();
        await process.WaitForExitAsync().WaitAsync(ShutdownTimeout);
        process.ExitCode.Should().Be(0, $"EOF must cause a clean exit; evidence: {artifacts}\n{Stderr}");
        // A parent exit alone does not prove descendants released inherited pipes.
        await Task.WhenAll(stdoutDrain, stderrDrain).WaitAsync(ShutdownTimeout);
        stopped = true;
    }

    public void AssertPureStdout()
    {
        stopped.Should().BeTrue("purity is only conclusive after exit and both completed stream drains");
        lock (violations) violations.Should().BeEmpty($"stdout must contain only complete MCP frames; evidence: {artifacts}");
        while (frames.Reader.TryRead(out var frame))
            frame.TryGetProperty("id", out _).Should().BeFalse("unsolicited or duplicate responses are forbidden");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            await StopAsync();
            AssertPureStdout();
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(ShutdownTimeout);
                }
                await Task.WhenAll(stdoutDrain, stderrDrain).WaitAsync(ShutdownTimeout);
            }
            finally
            {
                // Detached descendants cannot be reclaimed portably after the direct child exits.
                // Cancel our readers rather than hanging the runner; incomplete drains never pass.
                readersCancellation.Cancel();
                try { await Task.WhenAll(stdoutDrain, stderrDrain).WaitAsync(TimeSpan.FromSeconds(2)); }
                finally
                {
                    lock (stdout) File.WriteAllText(Path.Combine(artifacts, "stdout.log"), stdout.ToString());
                    lock (stderr) File.WriteAllText(Path.Combine(artifacts, "stderr.log"), stderr.ToString());
                    File.WriteAllText(Path.Combine(artifacts, "client.jsonl"), client.ToString());
                    process.Dispose();
                    readersCancellation.Dispose();
                }
            }
        }
    }
}
