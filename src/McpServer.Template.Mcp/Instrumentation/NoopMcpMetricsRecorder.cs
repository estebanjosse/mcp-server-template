namespace McpServer.Template.Mcp.Instrumentation;

public sealed class NoopMcpMetricsRecorder : IMcpMetricsRecorder
{
    public void RecordRequest()
    {
    }

    public void IncrementActiveSessions()
    {
    }

    public void DecrementActiveSessions()
    {
    }

    public void RecordToolInvocation(string toolName)
    {
    }
}
