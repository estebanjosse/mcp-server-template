namespace McpServer.Template.Mcp.Instrumentation;

public interface IMcpMetricsRecorder
{
    void RecordRequest();

    void IncrementRequestsInFlight();

    void DecrementRequestsInFlight();

    void RecordToolInvocation(string toolName);
}
