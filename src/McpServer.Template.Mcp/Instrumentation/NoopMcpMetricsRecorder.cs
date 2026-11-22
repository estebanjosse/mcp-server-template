namespace McpServer.Template.Mcp.Instrumentation;

public sealed class NoopMcpMetricsRecorder : IMcpMetricsRecorder
{
    public void RecordRequest()
    {
    }

    public void IncrementRequestsInFlight()
    {
    }

    public void DecrementRequestsInFlight()
    {
    }

    public void RecordToolInvocation(string toolName)
    {
    }
}
