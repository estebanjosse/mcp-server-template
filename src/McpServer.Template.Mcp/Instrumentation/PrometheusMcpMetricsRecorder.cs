using System.Threading;
using Prometheus;

namespace McpServer.Template.Mcp.Instrumentation;

public sealed class PrometheusMcpMetricsRecorder(IMetricFactory metricFactory) : IMcpMetricsRecorder
{
    private readonly Counter _requestsCounter = metricFactory.CreateCounter(
        "mcp_requests_total",
        "Total MCP HTTP requests processed by the host.");

    private readonly Counter _toolInvocationsCounter = metricFactory.CreateCounter(
        "mcp_tool_invocations_total",
        "Total count of MCP tool invocations.");

    private readonly Gauge _activeSessionsGauge = metricFactory.CreateGauge(
        "mcp_sessions_active",
        "Number of active MCP sessions handled by the host.");

    private long _activeSessions;

    public void RecordRequest()
    {
        _requestsCounter.Inc();
    }

    public void IncrementActiveSessions()
    {
        var current = Interlocked.Increment(ref _activeSessions);
        _activeSessionsGauge.Set(current);
    }

    public void DecrementActiveSessions()
    {
        var current = Interlocked.Decrement(ref _activeSessions);
        if (current < 0)
        {
            Interlocked.Exchange(ref _activeSessions, 0);
            current = 0;
        }

        _activeSessionsGauge.Set(current);
    }

    public void RecordToolInvocation(string toolName)
    {
        _ = toolName;
        _toolInvocationsCounter.Inc();
    }
}
