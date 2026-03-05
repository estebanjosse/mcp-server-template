using Microsoft.AspNetCore.Http;

namespace McpServer.Template.Host.Http.Authentication;

public interface IMcpAuthStrategy
{
    Task<AuthResult> AuthenticateAsync(HttpContext context);
}
