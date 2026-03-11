namespace McpServer.Template.Host.Http.Authentication;

public sealed record AuthResult(bool IsAuthenticated, string? FailureReason = null)
{
    public static AuthResult Success() => new(true);
    public static AuthResult Denied(string reason) => new(false, reason);
}
