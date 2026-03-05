using McpServer.Template.Host.Http.Authentication;

namespace McpServer.Template.Host.Http.Extensions;

public static class AuthenticationAppExtensions
{
    public static WebApplication UseMcpAuthentication(this WebApplication app)
    {
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/mcp"),
            branch => branch.UseMiddleware<AuthenticationMiddleware>());

        return app;
    }
}
