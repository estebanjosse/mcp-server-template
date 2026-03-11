using Microsoft.AspNetCore.Http;

namespace McpServer.Template.Host.Http.Authentication;

public sealed class SecurePlaceholderStrategy : IMcpAuthStrategy
{
    public async Task<AuthResult> AuthenticateAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status501NotImplemented;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(
            "The 'secure' authentication mode requires the secure auth package, which is not yet implemented.");

        return AuthResult.Denied("secure_not_implemented");
    }
}
