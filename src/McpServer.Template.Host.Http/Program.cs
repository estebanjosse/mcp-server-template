using McpServer.Template.Host.Http.Options;
using McpServer.Template.Mcp.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ModelContextProtocol.AspNetCore;
using Prometheus;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add MCP server with all modules
builder.Services.AddMcpTemplateModules();
builder.Services.AddMcpServer()
    .WithHttpTransport();

// Add health check services
builder.Services.AddHealthChecks();

var configuration = builder.Configuration;

builder.Services.AddOptions<MetricsOptions>()
    .Bind(configuration.GetSection("Metrics"))
    .PostConfigure(options =>
    {
        var envOverride = configuration["MCP_METRICS_ENABLED"];
        if (bool.TryParse(envOverride, out var envEnabled))
        {
            options.Enabled = envEnabled;
        }
    });

var metricsEnabled = configuration.GetValue<bool>("Metrics:Enabled");

if (metricsEnabled)
{
    builder.Services.AddSingleton(_ => Metrics.NewCustomRegistry());
    builder.Services.AddSingleton<IMetricFactory>(sp =>
    {
        var registry = sp.GetRequiredService<CollectorRegistry>();
        return Metrics.WithCustomRegistry(registry);
    });
}

var app = builder.Build();

// Map MCP endpoint with HTTP/SSE transport
app.MapMcp("/mcp");

// Map health check endpoint with JSON response
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();

