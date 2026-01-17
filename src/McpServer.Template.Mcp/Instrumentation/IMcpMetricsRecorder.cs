namespace McpServer.Template.Mcp.Instrumentation;

public interface IMcpMetricsRecorder
{
    void RecordRequest();

    void IncrementActiveSessions();

    void DecrementActiveSessions();

    void RecordToolInvocation(string toolName);
}
