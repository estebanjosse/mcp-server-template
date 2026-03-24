namespace McpServer.Template.Host.Http.Options;

public sealed class TransportSecurityOptions
{
    public bool ForwardedHeadersEnabled { get; set; }
    public int ForwardLimit { get; set; } = 1;
    public string[] KnownProxies { get; set; } = [];
}