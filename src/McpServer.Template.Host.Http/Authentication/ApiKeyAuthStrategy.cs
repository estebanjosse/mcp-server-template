using System.Security.Cryptography;
using System.Text;
using McpServer.Template.Host.Http.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Authentication;

public sealed class ApiKeyAuthStrategy : IMcpAuthStrategy
{
    private readonly byte[][] _keyBytes;
    private readonly string? _customHeader;

    public ApiKeyAuthStrategy(IOptions<AuthenticationOptions> options)
    {
        var opts = options.Value;
        _keyBytes = opts.ApiKeys.Select(k => Encoding.UTF8.GetBytes(k)).ToArray();
        _customHeader = opts.HeaderName;
    }

    public Task<AuthResult> AuthenticateAsync(HttpContext context)
    {
        var presented = ExtractCredential(context);

        if (string.IsNullOrEmpty(presented))
            return Task.FromResult(AuthResult.Denied("missing_credential"));

        var presentedBytes = Encoding.UTF8.GetBytes(presented);

        // Compare against ALL keys without short-circuiting to prevent timing leaks
        var matched = false;
        foreach (var storedKey in _keyBytes)
        {
            // Pad to same length for constant-time comparison
            var maxLen = Math.Max(presentedBytes.Length, storedKey.Length);
            var a = new byte[maxLen];
            var b = new byte[maxLen];
            presentedBytes.CopyTo(a, 0);
            storedKey.CopyTo(b, 0);

            if (CryptographicOperations.FixedTimeEquals(a, b)
                && presentedBytes.Length == storedKey.Length)
            {
                matched = true;
            }
        }

        return Task.FromResult(matched
            ? AuthResult.Success()
            : AuthResult.Denied("invalid_credential"));
    }

    private string? ExtractCredential(HttpContext context)
    {
        if (_customHeader is not null)
        {
            return context.Request.Headers[_customHeader].FirstOrDefault();
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        if (authorization is null || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authorization["Bearer ".Length..];
    }
}
