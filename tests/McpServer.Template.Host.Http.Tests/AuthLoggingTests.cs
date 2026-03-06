using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Template.Host.Http.Tests;

public sealed class AuthLoggingTests
{
    private const string ValidKey = "test-api-key-that-is-at-least-32-chars!";

    [Fact]
    public async Task SuccessfulAuth_LogsExpectedFields_AtInformation()
    {
        var logSink = new TestLogSink();
        using var factory = CreateFactory(logSink);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", ValidKey);
        await client.SendAsync(request);

        var authLogs = logSink.Entries
            .Where(e => e.Category.Contains("AuthenticationMiddleware"))
            .ToList();

        authLogs.Should().ContainSingle();
        var entry = authLogs[0];
        entry.LogLevel.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain("success");
        entry.Message.Should().Contain("/mcp");
        entry.Message.Should().Contain("Simple");
    }

    [Fact]
    public async Task FailedAuth_LogsExpectedFields_AtWarning()
    {
        var logSink = new TestLogSink();
        using var factory = CreateFactory(logSink);
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", "wrong-key-wrong-key-wrong-key-wrong!");
        await client.SendAsync(request);

        var authLogs = logSink.Entries
            .Where(e => e.Category.Contains("AuthenticationMiddleware"))
            .ToList();

        authLogs.Should().ContainSingle();
        var entry = authLogs[0];
        entry.LogLevel.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("denied");
        entry.Message.Should().Contain("/mcp");
        entry.Message.Should().Contain("invalid_credential");
    }

    [Fact]
    public async Task LogEntries_NeverContainKeyMaterial()
    {
        var logSink = new TestLogSink();
        using var factory = CreateFactory(logSink);
        using var client = factory.CreateClient();

        // Successful request
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req1.Headers.Authorization = new("Bearer", ValidKey);
        await client.SendAsync(req1);

        // Failed request with a recognizable wrong key
        var wrongKey = "recognizable-wrong-key-at-least-32chars";
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req2.Headers.Authorization = new("Bearer", wrongKey);
        await client.SendAsync(req2);

        var allMessages = string.Join("\n", logSink.Entries.Select(e => e.Message));

        allMessages.Should().NotContain(ValidKey);
        allMessages.Should().NotContain(wrongKey);
    }

    [Fact]
    public async Task BruteForce_AddsRetryAfterHeader()
    {
        var logSink = new TestLogSink();
        using var factory = CreateFactory(logSink);
        using var client = factory.CreateClient();

        // Send 6 failed requests to trigger brute-force
        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 7; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new("Bearer", "wrong-key-wrong-key-wrong-key-wrong!");
            lastResponse = await client.SendAsync(request);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        lastResponse.Headers.TryGetValues("Retry-After", out var retryValues).Should().BeTrue();
        int.Parse(retryValues!.First()).Should().BeGreaterThan(0);
    }

    private static WebApplicationFactory<Program> CreateFactory(TestLogSink sink)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, c) =>
            {
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "simple",
                    ["Authentication:ApiKeys:0"] = ValidKey
                });
            });
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new TestLoggerProvider(sink));
                    logging.SetMinimumLevel(LogLevel.Trace);
                });
            });
        });
    }
}

internal sealed class TestLogSink
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _lock = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get
        {
            lock (_lock)
                return _entries.ToList();
        }
    }

    public void Add(LogEntry entry)
    {
        lock (_lock)
            _entries.Add(entry);
    }
}

internal sealed record LogEntry(string Category, LogLevel LogLevel, string Message);

internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogSink _sink;
    public TestLoggerProvider(TestLogSink sink) => _sink = sink;
    public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName, _sink);
    public void Dispose() { }
}

internal sealed class TestLogger : ILogger
{
    private readonly string _category;
    private readonly TestLogSink _sink;

    public TestLogger(string category, TestLogSink sink)
    {
        _category = category;
        _sink = sink;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _sink.Add(new LogEntry(_category, logLevel, formatter(state, exception)));
    }
}
