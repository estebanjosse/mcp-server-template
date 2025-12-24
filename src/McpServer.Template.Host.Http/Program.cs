using McpServer.Template.Mcp.Extensions;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add MCP server with all modules
builder.Services.AddMcpTemplateModules();

var app = builder.Build();

// Map MCP endpoint with HTTP/SSE transport
app.MapMcp("/mcp");

app.Run();
