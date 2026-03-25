namespace McpServer.Template.Host.Http.Options;

public sealed class CorsOptions
{
    public string PolicyName { get; set; } = "mcp-browser";
    public string[] AllowedOrigins { get; set; } = [];
    public bool AllowCredentials { get; set; }
}