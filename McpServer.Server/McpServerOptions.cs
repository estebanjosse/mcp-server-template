using System.ComponentModel.DataAnnotations;

namespace McpServer.Server;

/// <summary>
/// Configuration options for the MCP server.
/// </summary>
public class McpServerOptions
{
    public const string SectionName = "McpServer";

    /// <summary>
    /// Gets or sets the transport type (stdio or http).
    /// </summary>
    [Required]
    [RegularExpression("^(stdio|http)$", ErrorMessage = "Transport must be either 'stdio' or 'http'")]
    public string Transport { get; set; } = "stdio";

    /// <summary>
    /// Gets or sets the HTTP port (only used when Transport is 'http').
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int HttpPort { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the server name.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Server name is required")]
    public string ServerName { get; set; } = "MCP Server";

    /// <summary>
    /// Gets or sets the server version.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "Server version is required")]
    public string ServerVersion { get; set; } = "1.0.0";
}
