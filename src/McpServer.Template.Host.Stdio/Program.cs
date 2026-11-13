using McpServer.Template.Mcp.Extensions;
using McpServer.Template.Mcp.Instrumentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(args);

// Reserve stdout for MCP frames; retain console diagnostics on stderr.
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

// Stdio has no HTTP metrics endpoint, but sample tools still require a recorder.
builder.Services.AddSingleton<IMcpMetricsRecorder, NoopMcpMetricsRecorder>();

// Add MCP server with all modules
builder.Services.AddMcpTemplateModules();
builder.Services.AddMcpServer()
    .WithStdioServerTransport();

var host = builder.Build();
await host.RunAsync();
