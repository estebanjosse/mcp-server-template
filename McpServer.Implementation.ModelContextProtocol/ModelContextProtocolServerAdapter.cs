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

namespace McpServer.Implementation.ModelContextProtocol;

/// <summary>
/// MCP server implementation that adapts our stable abstractions to the Model Context Protocol.
/// 
/// ARCHITECTURE NOTES:
/// - This layer wraps the mcpdotnet SDK (or any MCP SDK) without exposing SDK types
/// - All SDK-specific code is contained in this assembly
/// - The SDK can be replaced without affecting consumers of McpServer.Abstractions
/// - Transports (stdio/http) are handled by the SDK
/// 
/// RESPONSIBILITIES:
/// 1. Initialize MCP SDK with server metadata
/// 2. Register tools, prompts, and resources by adapting our interfaces to SDK expectations
/// 3. Handle transport layer (stdio, http/SSE) through SDK
/// 4. Translate between our stable API contracts and SDK formats
/// </summary>
public class ModelContextProtocolServerAdapter : IMcpServer
{
    private readonly IEnumerable<ITool> _tools;
    private readonly IEnumerable<IPrompt> _prompts;
    private readonly IEnumerable<IResource> _resources;
    private readonly McpServerOptions _options;
    private readonly ILogger<ModelContextProtocolServerAdapter> _logger;

    public ModelContextProtocolServerAdapter(
        IEnumerable<ITool> tools,
        IEnumerable<IPrompt> prompts,
        IEnumerable<IResource> resources,
        IOptions<McpServerOptions> options,
        ILogger<ModelContextProtocolServerAdapter> logger)
    {
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== MCP Server Initialization ===");
        _logger.LogInformation("Implementation: ModelContextProtocol (mcpdotnet SDK wrapper)");
        _logger.LogInformation("Server: {ServerName} v{ServerVersion}", _options.ServerName, _options.ServerVersion);
        _logger.LogInformation("Transport: {Transport}", _options.Transport);

        LogRegisteredCapabilities();

        // SDK Integration Point: Initialize MCP server with our adapted handlers
        // In production, this would use mcpdotnet SDK's builder pattern:
        // var mcpServer = MCPServer.CreateBuilder()
        //     .WithServerInfo(_options.ServerName, _options.ServerVersion)
        //     .WithTools(AdaptToolsForSdk())
        //     .WithPrompts(AdaptPromptsForSdk())
        //     .WithResources(AdaptResourcesForSdk())
        //     .Build();

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
            throw new InvalidOperationException($"Unsupported transport: {_options.Transport}");
        }
    }

    private void LogRegisteredCapabilities()
    {
        _logger.LogInformation("=== Registered Capabilities ===");
        
        _logger.LogInformation("Tools ({Count}):", _tools.Count());
        foreach (var tool in _tools)
        {
            _logger.LogInformation("  • {Name}: {Description}", tool.Name, tool.Description);
        }
        
        _logger.LogInformation("Prompts ({Count}):", _prompts.Count());
        foreach (var prompt in _prompts)
        {
            _logger.LogInformation("  • {Name}: {Description}", prompt.Name, prompt.Description);
        }
        
        _logger.LogInformation("Resources ({Count}):", _resources.Count());
        foreach (var resource in _resources)
        {
            _logger.LogInformation("  • {Uri} ({Name})", resource.Uri, resource.Name);
        }
        
        _logger.LogInformation("================================");
    }

    /// <summary>
    /// Runs MCP server over stdio transport using JSON-RPC 2.0.
    /// This demonstrates the MCP protocol flow while maintaining clean architecture.
    /// </summary>
    private async Task RunStdioServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting STDIO transport (JSON-RPC over stdin/stdout)");
        
        // In production: await mcpServer.RunStdioAsync(cancellationToken);
        // For now, implementing MCP protocol directly to demonstrate the architecture
        
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                _logger.LogDebug("Received request: {Request}", line);
                var response = await ProcessMcpRequestAsync(line, cancellationToken);
                _logger.LogDebug("Sending response: {Response}", response);
                
                await writer.WriteLineAsync(response);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MCP request");
            }
        }
        
        _logger.LogInformation("STDIO transport stopped");
    }

    /// <summary>
    /// Runs MCP server over HTTP transport using Server-Sent Events (SSE).
    /// This would use the SDK's HTTP server implementation.
    /// </summary>
    private async Task RunHttpServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting HTTP/SSE transport on port {Port}", _options.HttpPort);
        _logger.LogInformation("MCP endpoint would be: http://localhost:{Port}/mcp", _options.HttpPort);
        
        // In production with SDK:
        // await mcpServer.RunHttpAsync(_options.HttpPort, cancellationToken);
        
        // Placeholder: Keep server alive
        _logger.LogWarning("HTTP/SSE transport requires AspNetCore integration with mcpdotnet SDK");
        _logger.LogInformation("For full HTTP support, the SDK handles SSE streaming of MCP protocol messages");
        
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <summary>
    /// Processes MCP protocol requests (JSON-RPC 2.0).
    /// This method adapts our abstractions to MCP protocol format.
    /// </summary>
    private async Task<string> ProcessMcpRequestAsync(string requestJson, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(requestJson);
            var root = doc.RootElement;
            
            var method = root.GetProperty("method").GetString();
            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : 0;
            
            object? result = method switch
            {
                // MCP Protocol: Initialization
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
                        tools = new { },      // Server supports tools
                        prompts = new { },    // Server supports prompts
                        resources = new { }   // Server supports resources
                    }
                },
                
                // MCP Protocol: Tools
                "tools/list" => AdaptToolsList(),
                "tools/call" => await AdaptToolCallAsync(root, cancellationToken),
                
                // MCP Protocol: Prompts
                "prompts/list" => AdaptPromptsList(),
                "prompts/get" => await AdaptPromptGetAsync(root, cancellationToken),
                
                // MCP Protocol: Resources
                "resources/list" => AdaptResourcesList(),
                "resources/read" => await AdaptResourceReadAsync(root, cancellationToken),
                
                _ => throw new InvalidOperationException($"Unknown MCP method: {method}")
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
            _logger.LogError(ex, "Error processing MCP request");
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

    #region MCP Protocol Adapters
    
    // These methods translate between our stable abstractions and MCP protocol format
    // They demonstrate the Dependency Inversion Principle: SDK details don't leak out

    private object AdaptToolsList()
    {
        return new
        {
            tools = _tools.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                inputSchema = t.InputSchema
            }).ToArray()
        };
    }

    private async Task<object> AdaptToolCallAsync(JsonElement root, CancellationToken cancellationToken)
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

    private object AdaptPromptsList()
    {
        return new
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
        };
    }

    private async Task<object> AdaptPromptGetAsync(JsonElement root, CancellationToken cancellationToken)
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

    private object AdaptResourcesList()
    {
        return new
        {
            resources = _resources.Select(r => new
            {
                uri = r.Uri,
                name = r.Name,
                description = r.Description,
                mimeType = r.MimeType
            }).ToArray()
        };
    }

    private async Task<object> AdaptResourceReadAsync(JsonElement root, CancellationToken cancellationToken)
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

    #endregion
}
