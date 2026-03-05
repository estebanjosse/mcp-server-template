namespace McpServer.Template.Host.Http.Options;

public enum AuthenticationMode
{
    None,
    Simple,
    Secure
}

public sealed class AuthenticationOptions
{
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.None;
    public string[] ApiKeys { get; set; } = [];
    public string? HeaderName { get; set; }
}
