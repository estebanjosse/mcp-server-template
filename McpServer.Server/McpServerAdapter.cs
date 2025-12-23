using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Server;

/// <summary>
/// Implementation of the MCP server that adapts abstractions to a simple JSON-RPC based protocol.
/// This is a simplified implementation demonstrating clean architecture principles.
/// In production, this would wrap the official MCP SDK.
/// </summary>
public class McpServerAdapter : IMcpServer
{
    private readonly IEnumerable<ITool> _tools;
    private readonly IEnumerable<IPrompt> _prompts;
    private readonly IEnumerable<IResource> _resources;
    private readonly McpServerOptions _options;
    private readonly ILogger<McpServerAdapter> _logger;

    public McpServerAdapter(
        IEnumerable<ITool> tools,
        IEnumerable<IPrompt> prompts,
        IEnumerable<IResource> resources,
        IOptions<McpServerOptions> options,
        ILogger<McpServerAdapter> logger)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting MCP Server");
        _logger.LogInformation("Transport: {Transport}", _options.Transport);
        _logger.LogInformation("Server Name: {ServerName}", _options.ServerName);
        _logger.LogInformation("Server Version: {ServerVersion}", _options.ServerVersion);
        
        // Log registered capabilities
        _logger.LogInformation("Registered Tools: {ToolCount}", _tools.Count());
        foreach (var tool in _tools)
        {
            _logger.LogInformation("  - {ToolName}: {ToolDescription}", tool.Name, tool.Description);
        }
        
        _logger.LogInformation("Registered Prompts: {PromptCount}", _prompts.Count());
        foreach (var prompt in _prompts)
        {
            _logger.LogInformation("  - {PromptName}: {PromptDescription}", prompt.Name, prompt.Description);
        }
        
        _logger.LogInformation("Registered Resources: {ResourceCount}", _resources.Count());
        foreach (var resource in _resources)
        {
            _logger.LogInformation("  - {ResourceUri}: {ResourceName}", resource.Uri, resource.Name);
        }

        if (_options.Transport.ToLowerInvariant() == "stdio")
        {
            await RunStdioServerAsync(cancellationToken);
        }
        else if (_options.Transport.ToLowerInvariant() == "http")
        {
            await RunHttpServerAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Unknown transport type: {_options.Transport}");
        }
    }

    private async Task RunStdioServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Server running on STDIO transport");
        _logger.LogInformation("Waiting for JSON-RPC requests on standard input...");
        
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                var response = await ProcessRequestAsync(line, cancellationToken);
                await writer.WriteLineAsync(response);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
            }
        }
        
        _logger.LogInformation("MCP Server stopped");
    }

    private async Task RunHttpServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Server running on HTTP transport (SSE) on port {Port}", _options.HttpPort);
        _logger.LogInformation("HTTP server would listen on http://localhost:{Port}/mcp", _options.HttpPort);
        _logger.LogInformation("This is a placeholder - full HTTP/SSE implementation would require AspNetCore");
        
        // In a real implementation, this would start an HTTP server using AspNetCore
        // For now, just keep the server alive
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task<string> ProcessRequestAsync(string requestJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestJson);
            var root = doc.RootElement;
            
            var method = root.GetProperty("method").GetString();
            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
            
            object? result = method switch
            {
                "initialize" => new
                {
                    protocolVersion = "2024-11-05",
                    serverInfo = new
                    {
                        name = _options.ServerName,
                        version = _options.ServerVersion
                    },
                    capabilities = new
                    {
                        tools = new { },
                        prompts = new { },
                        resources = new { }
                    }
                },
                "tools/list" => new
                {
                    tools = _tools.Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        inputSchema = t.InputSchema
                    }).ToArray()
                },
                "tools/call" => await HandleToolCall(root, cancellationToken),
                "prompts/list" => new
                {
                    prompts = _prompts.Select(p => new
                    {
                        name = p.Name,
                        description = p.Description,
                        arguments = p.Arguments.Select(a => new
                        {
                            name = a.Name,
                            description = a.Description,
                            required = a.Required
                        }).ToArray()
                    }).ToArray()
                },
                "prompts/get" => await HandlePromptGet(root, cancellationToken),
                "resources/list" => new
                {
                    resources = _resources.Select(r => new
                    {
                        uri = r.Uri,
                        name = r.Name,
                        description = r.Description,
                        mimeType = r.MimeType
                    }).ToArray()
                },
                "resources/read" => await HandleResourceRead(root, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown method: {method}")
            };

            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id,
                result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request");
            return JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32603,
                    message = ex.Message
                }
            });
        }
    }

    private async Task<object> HandleToolCall(JsonElement root, CancellationToken cancellationToken)
    {
        var @params = root.GetProperty("params");
        var toolName = @params.GetProperty("name").GetString();
        var tool = _tools.FirstOrDefault(t => t.Name == toolName);
        
        if (tool == null)
        {
            return new { content = new[] { new { type = "text", text = $"Tool not found: {toolName}" } }, isError = true };
        }

        var arguments = new Dictionary<string, object?>();
        if (@params.TryGetProperty("arguments", out var argsElement))
        {
            foreach (var prop in argsElement.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.GetRawText()
                };
            }
        }

        var result = await tool.ExecuteAsync(arguments, cancellationToken);
        return new
        {
            content = new[] { new { type = "text", text = result.Content } },
            isError = result.IsError
        };
    }

    private async Task<object> HandlePromptGet(JsonElement root, CancellationToken cancellationToken)
    {
        var @params = root.GetProperty("params");
        var promptName = @params.GetProperty("name").GetString();
        var prompt = _prompts.FirstOrDefault(p => p.Name == promptName);
        
        if (prompt == null)
        {
            return new { messages = new[] { new { role = "user", content = new { type = "text", text = $"Prompt not found: {promptName}" } } } };
        }

        var arguments = new Dictionary<string, string>();
        if (@params.TryGetProperty("arguments", out var argsElement))
        {
            foreach (var prop in argsElement.EnumerateObject())
            {
                arguments[prop.Name] = prop.Value.GetString() ?? "";
            }
        }

        var messages = await prompt.GetMessagesAsync(arguments, cancellationToken);
        return new
        {
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = new { type = "text", text = m.Content }
            }).ToArray()
        };
    }

    private async Task<object> HandleResourceRead(JsonElement root, CancellationToken cancellationToken)
    {
        var @params = root.GetProperty("params");
        var resourceUri = @params.GetProperty("uri").GetString();
        var resource = _resources.FirstOrDefault(r => r.Uri == resourceUri);
        
        if (resource == null)
        {
            return new { contents = new[] { new { uri = resourceUri, mimeType = "text/plain", text = $"Resource not found: {resourceUri}" } } };
        }

        var content = await resource.ReadAsync(cancellationToken);
        return new
        {
            contents = new[]
            {
                new
                {
                    uri = resource.Uri,
                    mimeType = resource.MimeType,
                    text = content
                }
            }
        };
    }
}
