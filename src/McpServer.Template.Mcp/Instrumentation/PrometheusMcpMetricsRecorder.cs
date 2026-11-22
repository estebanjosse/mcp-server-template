using Prometheus;

namespace McpServer.Template.Mcp.Instrumentation;

public sealed class PrometheusMcpMetricsRecorder(IMetricFactory metricFactory) : IMcpMetricsRecorder
{
    private const string UnknownToolLabel = "unknown";

    private readonly Counter _requestsCounter = metricFactory.CreateCounter(
        "mcp_requests_total",
        "Total MCP HTTP requests processed by the host.");

    private readonly Counter _toolInvocationsCounter = metricFactory.CreateCounter(
        "mcp_tool_invocations_total",
        "Total count of MCP tool invocations.");

    private readonly Counter _toolInvocationsByToolCounter = metricFactory.CreateCounter(
        "mcp_tool_invocations_by_tool_total",
        "Count of MCP tool invocations by tool.",
        new CounterConfiguration
        {
            LabelNames = ["tool"]
        });

    private readonly Gauge _requestsInFlightGauge = metricFactory.CreateGauge(
        "mcp_requests_in_flight",
        "Number of admitted MCP HTTP requests currently in flight.");

    public void RecordRequest()
    {
        _requestsCounter.Inc();
    }

    public void IncrementRequestsInFlight() => _requestsInFlightGauge.Inc();

    public void DecrementRequestsInFlight() => _requestsInFlightGauge.Dec();

    public void RecordToolInvocation(string toolName)
    {
        _toolInvocationsCounter.Inc();
        _toolInvocationsByToolCounter.WithLabels(NormalizeToolLabel(toolName)).Inc();
    }

    private static string NormalizeToolLabel(string toolName) => string.IsNullOrWhiteSpace(toolName)
        ? UnknownToolLabel
        : toolName.Trim();
}
