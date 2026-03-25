namespace McpServer.Template.Host.Http.Options;

public enum RateLimitIdentitySource
{
    ClientIp,
    ApiKey
}

public sealed class RateLimitingOptions
{
    public bool Enabled { get; set; }
    public string PolicyName { get; set; } = "mcp-per-client";
    public RateLimitIdentitySource Identity { get; set; } = RateLimitIdentitySource.ClientIp;
    public int PermitLimit { get; set; } = 60;
    public int WindowSeconds { get; set; } = 60;
    public int QueueLimit { get; set; }
}