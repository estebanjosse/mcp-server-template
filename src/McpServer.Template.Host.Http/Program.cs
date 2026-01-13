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

var metricsEnabled = ShouldEnableMetrics(configuration);

if (metricsEnabled)
{
    builder.Services.AddSingleton(Metrics.DefaultRegistry);
    builder.Services.AddSingleton<IMetricFactory>(_ => Metrics.DefaultFactory);
}

var app = builder.Build();

if (metricsEnabled)
{
    app.UseHttpMetrics();
    var registry = app.Services.GetRequiredService<CollectorRegistry>();
    app.MapMetrics("/metrics", registry);
}

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

static bool ShouldEnableMetrics(ConfigurationManager configuration)
{
    var envOverride = configuration["MCP_METRICS_ENABLED"];
    if (bool.TryParse(envOverride, out var envEnabled))
    {
        return envEnabled;
    }

    return configuration.GetValue<bool>("Metrics:Enabled");
}

