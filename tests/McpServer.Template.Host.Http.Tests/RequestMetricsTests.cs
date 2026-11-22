using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Template.Mcp.Instrumentation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Prometheus;

namespace McpServer.Template.Host.Http.Tests;

public sealed class RequestMetricsTests
{
    private const string Gauge = "mcp_requests_in_flight";
    private const string Counter = "mcp_requests_total";
    private const string FixtureKey = "metrics-tests-not-a-production-credential";

    [Fact]
    public async Task Gauge_TracksConcurrentRequestsAndReleasesOnSuccessAndCancellation()
    {
        var first = new Gate();
        var second = new Gate();
        using var factory = Host(handler: (context, token) => (context.Params?.Cursor == "first" ? first : second).WaitAsync(token));
        using var client = factory.CreateClient();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        using var a = Request("prompts/list", "first");
        using var b = Request("prompts/list", "second");
        var initial = await Scrape(client, deadline.Token);
        initial.Should().NotContain("mcp_sessions_active");
        Read(initial, Gauge).Should().Be(0);
        var pendingA = client.SendAsync(a, deadline.Token);
        var pendingB = client.SendAsync(b, cancel.Token);
        try
        {
            await Task.WhenAll(first.Entered.Task, second.Entered.Task).WaitAsync(deadline.Token);
            var running = await Scrape(client, deadline.Token);
            Read(running, Gauge).Should().Be(2);
            Read(running, Counter).Should().Be(Read(initial, Counter) + 2);
            first.Release.TrySetResult();
            using var completed = await pendingA;
            completed.EnsureSuccessStatusCode();
            await WaitForGauge(client, 1, deadline.Token);
            cancel.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingB.WaitAsync(deadline.Token));
            await second.Cancelled.Task.WaitAsync(deadline.Token);
            await WaitForGauge(client, 0, deadline.Token);
        }
        finally
        {
            first.Release.TrySetResult();
            second.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Metrics_CountAdmittedDiscoveryAndFailuresButNotAuthRejectionsOrMonitoring()
    {
        using var factory = Host(handler: (_, _) => throw new InvalidOperationException("Metrics fixture failure"));
        using var client = factory.CreateClient();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var token = deadline.Token;
        var initial = Read(await Scrape(client, token), Counter);
        using var noKey = Request("server/discover", authorized: false);
        using var rejected = await client.SendAsync(noKey, token);
        rejected.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var health = await client.GetAsync("/health", token);
        health.EnsureSuccessStatusCode();
        var untouched = await Scrape(client, token);
        Read(untouched, Counter).Should().Be(initial);
        Read(untouched, Gauge).Should().Be(0);
        using var discovery = Request("server/discover");
        using var discovered = await client.SendAsync(discovery, token);
        discovered.EnsureSuccessStatusCode();
        using var invalid = Request("server/discover", versionHeader: "invalid-version");
        using var sdkRejected = await client.SendAsync(invalid, token);
        sdkRejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var failure = Request("prompts/list");
        using var failed = await client.SendAsync(failure, token);
        (await failed.Content.ReadAsStringAsync(token)).Should().Contain("-32603");
        await WaitForGauge(client, 0, token);
        Read(await Scrape(client, token), Counter).Should().Be(initial + 3);
    }

    [Fact]
    public async Task Metrics_DisabledUsesNoopAndLeavesMcpAvailable()
    {
        using var factory = Host(enabled: false);
        using var client = factory.CreateClient();
        factory.Services.GetRequiredService<IMcpMetricsRecorder>().Should().BeOfType<NoopMcpMetricsRecorder>();
        factory.Services.GetService<CollectorRegistry>().Should().BeNull();
        using var scrape = await client.GetAsync("/metrics");
        scrape.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var discovery = Request("server/discover");
        using var response = await client.SendAsync(discovery);
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ToolCalls_PreserveResultsAndBusinessErrorsWithEitherRecorder(bool enabled)
    {
        // dotnet new replaces the flag; the source repository includes samples.
        if (string.Equals("__MCP_EXPECT_SAMPLE_TOOLS__", "false", StringComparison.OrdinalIgnoreCase)) return;
        using var factory = Host(enabled);
        using var client = factory.CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        const string toolCounter = "mcp_tool_invocations_total";
        var before = enabled ? Read(await Scrape(client, timeout.Token), toolCounter) : 0;
        using var request = Request("tools/call", toolArguments: new { a = 7, b = 2 });
        using var response = await client.SendAsync(request, timeout.Token);
        response.EnsureSuccessStatusCode();
        var result = (await McpProtocolBehaviorTests.ReadReply(response, 1)).GetProperty("result");
        var content = result.GetProperty("content")[0];
        using var dto = JsonDocument.Parse(content.GetProperty("text").GetString()!);
        dto.RootElement.GetProperty("result").GetDouble().Should().Be(3.5);
        using var divideByZero = Request("tools/call", toolArguments: new { a = 7, b = 0 });
        using var failed = await client.SendAsync(divideByZero, timeout.Token);
        failed.EnsureSuccessStatusCode();
        var failure = (await McpProtocolBehaviorTests.ReadReply(failed, 1)).GetProperty("result");
        failure.GetProperty("isError").GetBoolean().Should().BeTrue();
        failure.GetProperty("content")[0].GetProperty("text").GetString().Should()
            .Be("An error occurred invoking 'calc_divide': Division by zero is not allowed");
        if (enabled)
        {
            var payload = await Scrape(client, timeout.Token);
            Read(payload, toolCounter).Should().Be(before + 1);
            Read(payload, "mcp_tool_invocations_by_tool_total{tool=\"calc_divide\"}").Should().Be(1);
        }
    }

    private static WebApplicationFactory<Program> Host(bool enabled = true,
        McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>? handler = null) => new MetricsHost(enabled, handler);

    private sealed class MetricsHost(bool enabled, McpRequestHandler<ListPromptsRequestParams, ListPromptsResult>? handler)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment(enabled ? "Development" : "Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Mode"] = "simple",
                ["Authentication:ApiKeys:0"] = FixtureKey,
                ["Authentication:HeaderName"] = "X-Metrics-Test-Key"
            }));
            if (enabled)
                builder.ConfigureTestServices(services =>
                {
                    var registry = Metrics.NewCustomRegistry();
                    services.AddSingleton(registry);
                    services.AddSingleton<IMetricFactory>(Metrics.WithCustomRegistry(registry));
                });
            if (handler is not null)
                builder.ConfigureServices(services => services.AddMcpServer().WithListPromptsHandler(handler));
        }
    }

    private static HttpRequestMessage Request(string method, string? cursor = null, bool authorized = true,
        string versionHeader = "2026-07-28", object? toolArguments = null)
    {
        var parameters = new Dictionary<string, object>
        {
            ["_meta"] = new Dictionary<string, object>
            {
                ["io.modelcontextprotocol/protocolVersion"] = "2026-07-28",
                ["io.modelcontextprotocol/clientInfo"] = new { name = "metrics-tests", version = "1" },
                ["io.modelcontextprotocol/clientCapabilities"] = new { }
            }
        };
        if (cursor is not null) parameters["cursor"] = cursor;
        if (toolArguments is not null)
        {
            parameters["name"] = "calc_divide";
            parameters["arguments"] = toolArguments;
        }
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = 1, method, @params = parameters })
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", versionHeader);
        request.Headers.Add("Mcp-Method", method);
        if (toolArguments is not null) request.Headers.Add("Mcp-Name", "calc_divide");
        if (authorized) request.Headers.Add("X-Metrics-Test-Key", FixtureKey);
        return request;
    }

    private static async Task<string> Scrape(HttpClient client, CancellationToken token) => await client.GetStringAsync("/metrics", token);

    private static double Read(string payload, string name)
    {
        var line = Assert.Single(payload.Split('\n'), line => line.StartsWith(name + " ", StringComparison.Ordinal));
        return double.Parse(line.AsSpan(name.Length + 1), CultureInfo.InvariantCulture);
    }

    private static async Task WaitForGauge(HttpClient client, double expected, CancellationToken token)
    {
        while (Read(await Scrape(client, token), Gauge) != expected)
            await Task.Delay(25, token);
    }

    private sealed class Gate
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async ValueTask<ListPromptsResult> WaitAsync(CancellationToken token)
        {
            Entered.TrySetResult();
            try { await Release.Task.WaitAsync(token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                Cancelled.TrySetResult();
                throw;
            }
            return new ListPromptsResult { Prompts = [] };
        }
    }
}
