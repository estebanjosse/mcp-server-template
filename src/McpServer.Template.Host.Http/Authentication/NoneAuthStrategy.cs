using Microsoft.AspNetCore.Http;

namespace McpServer.Template.Host.Http.Authentication;

public sealed class NoneAuthStrategy : IMcpAuthStrategy
{
    public Task<AuthResult> AuthenticateAsync(HttpContext context)
        => Task.FromResult(AuthResult.Success());
}
