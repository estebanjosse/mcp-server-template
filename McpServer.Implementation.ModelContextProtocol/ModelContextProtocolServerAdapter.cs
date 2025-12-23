using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Implementation.ModelContextProtocol;

/// <summary>
/// MCP server implementation that adapts our stable abstractions to the Model Context Protocol.
/// 
/// ARCHITECTURE NOTES:
/// - This layer wraps the official ModelContextProtocol SDK without exposing SDK types
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
        _logger.LogInformation("Implementation: ModelContextProtocol SDK v0.6.0");
        _logger.LogInformation("Server: {ServerName} v{ServerVersion}", _options.ServerName, _options.ServerVersion);
        _logger.LogInformation("Transport: {Transport}", _options.Transport);

        LogRegisteredCapabilities();

        // Create MCP server options with handlers
        var serverOptions = CreateServerOptions();

        // Create server with appropriate transport
        if (_options.Transport.ToLowerInvariant() == "stdio")
        {
            await RunStdioServerAsync(serverOptions, cancellationToken);
        }
        else if (_options.Transport.ToLowerInvariant() == "http")
        {
            await RunHttpServerAsync(serverOptions, cancellationToken);
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
    /// Creates MCP server options with manual handler registration.
    /// This adapts our ITool/IPrompt/IResource abstractions to MCP SDK format.
    /// </summary>
    private global::ModelContextProtocol.Server.McpServerOptions CreateServerOptions()
    {
        var options = new global::ModelContextProtocol.Server.McpServerOptions
        {
            ServerInfo = new global::ModelContextProtocol.Protocol.Implementation 
            { 
                Name = _options.ServerName, 
                Version = _options.ServerVersion 
            },
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true },
                Resources = new ResourcesCapability { Subscribe = false, ListChanged = true },
                Prompts = new PromptsCapability { ListChanged = true }
            },
            Handlers = new McpServerHandlers
            {
                // Adapt Tools
                ListToolsHandler = (request, ct) =>
                {
                    var tools = _tools.Select(tool => new Tool
                    {
                        Name = tool.Name,
                        Description = tool.Description,
                        InputSchema = JsonSerializer.SerializeToElement(tool.InputSchema)
                    }).ToList();

                    return ValueTask.FromResult(new ListToolsResult { Tools = tools });
                },

                CallToolHandler = async (request, ct) =>
                {
                    var toolName = request.Params?.Name;
                    var tool = _tools.FirstOrDefault(t => t.Name == toolName);

                    if (tool == null)
                    {
                        throw new McpProtocolException(
                            $"Tool not found: {toolName}",
                            McpErrorCode.InvalidRequest);
                    }

                    // Convert SDK arguments to our format
                    var arguments = new Dictionary<string, object?>();
                    if (request.Params?.Arguments != null)
                    {
                        foreach (var prop in request.Params.Arguments)
                        {
                            arguments[prop.Key] = ConvertJsonElement(prop.Value);
                        }
                    }

                    // Execute tool
                    var result = await tool.ExecuteAsync(arguments, ct);

                    return new CallToolResult
                    {
                        Content = 
                        [
                            new global::ModelContextProtocol.Protocol.TextContentBlock
                            {
                                Text = result.Content
                            }
                        ],
                        IsError = result.IsError
                    };
                },

                // Adapt Prompts
                ListPromptsHandler = (request, ct) =>
                {
                    var prompts = _prompts.Select(prompt => new Prompt
                    {
                        Name = prompt.Name,
                        Description = prompt.Description,
                        Arguments = prompt.Arguments.Select(a => new global::ModelContextProtocol.Protocol.PromptArgument
                        {
                            Name = a.Name,
                            Description = a.Description,
                            Required = a.Required
                        }).ToList()
                    }).ToList();

                    return ValueTask.FromResult(new ListPromptsResult { Prompts = prompts });
                },

                GetPromptHandler = async (request, ct) =>
                {
                    var promptName = request.Params?.Name;
                    var prompt = _prompts.FirstOrDefault(p => p.Name == promptName);

                    if (prompt == null)
                    {
                        throw new McpProtocolException(
                            $"Prompt not found: {promptName}",
                            McpErrorCode.InvalidRequest);
                    }

                    // Convert SDK arguments to our format
                    var arguments = new Dictionary<string, string>();
                    if (request.Params?.Arguments != null)
                    {
                        foreach (var prop in request.Params.Arguments)
                        {
                            arguments[prop.Key] = prop.Value.ToString() ?? "";
                        }
                    }

                    // Execute prompt
                    var messages = await prompt.GetMessagesAsync(arguments, ct);

                    return new GetPromptResult
                    {
                        Messages = messages.Select(m => new global::ModelContextProtocol.Protocol.PromptMessage
                        {
                            Role = m.Role == "user" ? Role.User : Role.Assistant,
                            Content = new global::ModelContextProtocol.Protocol.TextContentBlock
                            {
                                Text = m.Content
                            }
                        }).ToList()
                    };
                },

                // Adapt Resources
                ListResourcesHandler = (request, ct) =>
                {
                    var resources = _resources.Select(resource => new Resource
                    {
                        Uri = resource.Uri,
                        Name = resource.Name,
                        Description = resource.Description,
                        MimeType = resource.MimeType
                    }).ToList();

                    return ValueTask.FromResult(new ListResourcesResult { Resources = resources });
                },

                ReadResourceHandler = async (request, ct) =>
                {
                    var uri = request.Params?.Uri;
                    var resource = _resources.FirstOrDefault(r => r.Uri == uri);

                    if (resource == null)
                    {
                        throw new McpProtocolException(
                            $"Resource not found: {uri}",
                            McpErrorCode.InvalidRequest);
                    }

                    // Read resource content
                    var content = await resource.ReadAsync(ct);

                    return new ReadResourceResult
                    {
                        Contents =
                        [
                            new global::ModelContextProtocol.Protocol.TextResourceContents
                            {
                                Uri = resource.Uri,
                                Text = content,
                                MimeType = resource.MimeType
                            }
                        ]
                    };
                }
            }
        };

        return options;
    }

    /// <summary>
    /// Converts JsonElement to appropriate .NET type for our tool arguments.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element // Return as-is for complex types
        };
    }

    /// <summary>
    /// Runs MCP server over stdio transport using the official SDK.
    /// </summary>
    private async Task RunStdioServerAsync(global::ModelContextProtocol.Server.McpServerOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting STDIO transport (JSON-RPC over stdin/stdout)");
        
        // Create server with stdio transport using the SDK
        var transport = new StdioServerTransport(_options.ServerName);
        await using var server = global::ModelContextProtocol.Server.McpServer.Create(transport, options);

        // Run server (blocks until client disconnects)
        await server.RunAsync(cancellationToken);
        
        _logger.LogInformation("STDIO transport stopped");
    }

    /// <summary>
    /// Runs MCP server over HTTP transport using Server-Sent Events (SSE).
    /// Note: HTTP transport requires ASP.NET Core integration.
    /// </summary>
    private Task RunHttpServerAsync(global::ModelContextProtocol.Server.McpServerOptions options, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting HTTP/SSE transport on port {Port}", _options.HttpPort);
        _logger.LogWarning("HTTP/SSE transport requires ASP.NET Core integration - not supported in console apps");
        _logger.LogInformation("For HTTP support, use ASP.NET Core with builder.Services.AddMcpServer().WithHttpTransport()");
        
        // HTTP transport requires ASP.NET Core, which is not available in console apps
        // Users should use the WithHttpTransport() extension in an ASP.NET Core app
        throw new NotSupportedException(
            "HTTP transport requires ASP.NET Core. Use stdio transport for console apps, " +
            "or create an ASP.NET Core app with AddMcpServer().WithHttpTransport()");
    }
}
