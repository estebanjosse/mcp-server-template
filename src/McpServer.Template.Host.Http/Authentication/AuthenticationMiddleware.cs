using McpServer.Template.Host.Http.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Template.Host.Http.Authentication;

public sealed partial class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMcpAuthStrategy _strategy;
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly AuthenticationMode _mode;
    private readonly BruteForceTracker _bruteForce;

    public AuthenticationMiddleware(
        RequestDelegate next,
        IMcpAuthStrategy strategy,
        ILogger<AuthenticationMiddleware> logger,
        IOptions<AuthenticationOptions> options,
        BruteForceTracker bruteForce)
    {
        _next = next;
        _strategy = strategy;
        _logger = logger;
        _mode = options.Value.Mode;
        _bruteForce = bruteForce;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var result = await _strategy.AuthenticateAsync(context);

        if (_mode != AuthenticationMode.None)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.Value ?? "/";

            if (result.IsAuthenticated)
            {
                LogAuthSuccess(_logger, clientIp, path, _mode);
                _bruteForce.RecordSuccess(clientIp);
            }
            else
            {
                LogAuthFailure(_logger, clientIp, path, _mode, result.FailureReason ?? "unknown");
                var retryAfter = _bruteForce.RecordFailure(clientIp);
                if (retryAfter is not null)
                {
                    context.Response.Headers["Retry-After"] = retryAfter.Value.ToString();
                }
            }
        }

        if (!result.IsAuthenticated)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers["WWW-Authenticate"] = "Bearer realm=\"MCP\"";
            }
            return;
        }

        await _next(context);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Auth success from {ClientIp} on {Path} mode={Mode}")]
    private static partial void LogAuthSuccess(
        ILogger logger, string clientIp, string path, AuthenticationMode mode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Auth denied from {ClientIp} on {Path} mode={Mode} reason={Reason}")]
    private static partial void LogAuthFailure(
        ILogger logger, string clientIp, string path, AuthenticationMode mode, string reason);
}
