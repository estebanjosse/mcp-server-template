using Microsoft.AspNetCore.Http;

namespace McpServer.Template.Host.Http.Authentication;

public sealed class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMcpAuthStrategy _strategy;

    public AuthenticationMiddleware(RequestDelegate next, IMcpAuthStrategy strategy)
    {
        _next = next;
        _strategy = strategy;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var result = await _strategy.AuthenticateAsync(context);

        if (!result.IsAuthenticated)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
