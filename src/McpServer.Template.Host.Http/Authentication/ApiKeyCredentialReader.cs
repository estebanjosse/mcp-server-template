using Microsoft.AspNetCore.Http;

namespace McpServer.Template.Host.Http.Authentication;

internal static class ApiKeyCredentialReader
{
    public static string? ExtractCredential(HttpContext context, string? customHeader)
    {
        if (!string.IsNullOrWhiteSpace(customHeader))
        {
            return context.Request.Headers[customHeader].FirstOrDefault();
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authorization["Bearer ".Length..];
    }
}