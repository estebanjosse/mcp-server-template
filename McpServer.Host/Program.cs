using McpServer.Abstractions;
using McpServer.Examples;
using McpServer.Implementation.ModelContextProtocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Build configuration from environment variables
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        // Default configuration
        ["McpServer:Transport"] = Environment.GetEnvironmentVariable("MCP_TRANSPORT") ?? "stdio",
        ["McpServer:HttpPort"] = Environment.GetEnvironmentVariable("MCP_HTTP_PORT") ?? "5000",
        ["McpServer:ServerName"] = Environment.GetEnvironmentVariable("MCP_SERVER_NAME") ?? "MCP Server",
        ["McpServer:ServerVersion"] = Environment.GetEnvironmentVariable("MCP_SERVER_VERSION") ?? "1.0.0"
    })
    .Build();

// Create and configure the host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add MCP server with configuration
        services.AddMcpServer(configuration);

        // Add all example implementations
        services.AddAllExamples();

        // Add hosted service to run the MCP server
        services.AddHostedService<McpServerHostedService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Run the host
await host.RunAsync();

/// <summary>
/// Hosted service that runs the MCP server.
/// </summary>
sealed class McpServerHostedService : IHostedService
{
    private readonly IMcpServer _mcpServer;
    private readonly ILogger<McpServerHostedService> _logger;
    private Task? _runTask;
    private CancellationTokenSource? _cts;

    public McpServerHostedService(IMcpServer mcpServer, ILogger<McpServerHostedService> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Server Hosted Service");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runTask = _mcpServer.RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Server Hosted Service");
        
        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_runTask != null)
        {
            try
            {
                await _runTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _cts?.Dispose();
    }
}
