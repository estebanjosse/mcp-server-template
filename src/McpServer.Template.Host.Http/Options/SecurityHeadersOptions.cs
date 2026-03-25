namespace McpServer.Template.Host.Http.Options;

public sealed class SecurityHeadersOptions
{
    public bool Enabled { get; set; } = true;
    public bool HstsEnabled { get; set; } = true;
    public int HstsMaxAgeSeconds { get; set; } = 31_536_000;
    public bool HstsIncludeSubDomains { get; set; } = true;
    public bool HstsPreload { get; set; }
    public bool XContentTypeOptionsEnabled { get; set; } = true;
    public bool XFrameOptionsEnabled { get; set; } = true;
    public string XFrameOptionsValue { get; set; } = "DENY";
    public string? ContentSecurityPolicy { get; set; }
}