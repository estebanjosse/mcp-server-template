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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "MCP Server",
    transport = "HTTP/SSE",
    timestamp = DateTime.UtcNow
}));

// Server information and capabilities endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "Model Context Protocol Server",
    transport = "HTTP/SSE",
    version = "1.0.0",
    endpoints = new
    {
        mcp = "/mcp (GET for SSE, POST for messages)",
        health = "/health",
        capabilities = "/capabilities"
    },
    documentation = "https://modelcontextprotocol.io"
}));

// List available capabilities (shows shared McpServer.Examples code)
app.MapGet("/capabilities", () =>
{
    var tools = new[]
    {
        new { name = "calculator", description = "Performs basic arithmetic operations (add, subtract, multiply, divide)" },
        new { name = "echo", description = "Echoes back the provided message" }
    };
    
    var prompts = new[]
    {
        new { name = "greeting", description = "Generates a personalized greeting message" },
        new { name = "code_review", description = "Provides a code review template" }
    };
    
    var resources = new[]
    {
        new { name = "welcome", uri = "welcome://info", description = "Welcome message with server information" },
        new { name = "status", uri = "status://server", description = "Current server status and statistics" }
    };
    
    return Results.Ok(new
    {
        message = "These capabilities are shared between Console (STDIO) and Web (HTTP/SSE) transports",
        sharedProject = "McpServer.Examples",
        tools,
        prompts,
        resources,
        note = "Adding a new tool/prompt/resource to McpServer.Examples makes it available in both transports automatically"
    });
});

app.Run();
