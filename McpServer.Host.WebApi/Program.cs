using McpServer.Examples.Tools;
using McpServer.Examples.Prompts;
using McpServer.Examples.Resources;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Configure MCP server with HTTP/SSE transport using the official SDK
builder.Services
    .AddMcpServer(options =>
    {
        // Server metadata
        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
        {
            Name = builder.Configuration["McpServer:ServerName"] ?? "MCP Server (HTTP)",
            Version = builder.Configuration["McpServer:ServerVersion"] ?? "1.0.0"
        };
    })
    .WithHttpTransport(httpOptions =>
    {
        // Configure HTTP/SSE transport options
        httpOptions.Stateless = false; // Enable stateful sessions
        httpOptions.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    })
    // Register tools, prompts, and resources from McpServer.Examples
    .WithTools<CalculatorTool>()
    .WithTools<EchoTool>()
    .WithPrompts<GreetingPrompt>()
    .WithPrompts<CodeReviewPrompt>()
    .WithResources<WelcomeResource>()
    .WithResources<ServerStatusResource>();

var app = builder.Build();

// Map MCP endpoints at /mcp
// The SDK automatically handles:
// - GET /mcp for SSE connection
// - POST /mcp for messages
app.MapMcp("/mcp");

// Optional: Add a health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "MCP Server",
    transport = "HTTP/SSE",
    timestamp = DateTime.UtcNow
}));

// Optional: Add a root endpoint with information
app.MapGet("/", () => Results.Ok(new
{
    message = "Model Context Protocol Server",
    transport = "HTTP/SSE",
    endpoints = new
    {
        mcp = "/mcp (GET for SSE, POST for messages)",
        health = "/health"
    },
    documentation = "https://modelcontextprotocol.io"
}));

app.Run();
