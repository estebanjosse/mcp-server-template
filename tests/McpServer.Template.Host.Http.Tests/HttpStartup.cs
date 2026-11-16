using System.Diagnostics;
using System.Text;

namespace McpServer.Template.Host.Http.Tests;

internal static class HttpStartup
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(5);

    internal static bool HasExpectedFailure(int exitCode, string output, string diagnostic) =>
        exitCode != 0
        && !output.Contains("ObjectDisposedException", StringComparison.Ordinal)
        && output.Split('\n').Any(line => line.Trim() == "Unhandled exception. System.InvalidOperationException: " + diagnostic);

    internal static async Task AssertRejectedAsync(string mode, string? environmentMode, string diagnostic)
    {
        var artifacts = Path.Combine(Environment.GetEnvironmentVariable("MCP_TEST_ARTIFACTS") ?? Path.GetTempPath(),
            $"http-startup-{mode}-{environmentMode ?? "config"}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(artifacts);
        var dll = Path.Combine(AppContext.BaseDirectory, "http", "McpServer.Template.Host.Http.dll");
        if (!File.Exists(dll)) throw new FileNotFoundException("Build the HTTP host before running its startup tests.", dll);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(dll)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        start.ArgumentList.Add("--Authentication:Mode");
        start.ArgumentList.Add(mode);
        // Isolate only the child's authentication settings, including prefixed configuration providers.
        foreach (var key in start.Environment.Keys.ToArray())
        {
            var normalized = key.Replace("__", ":", StringComparison.Ordinal);
            if (normalized.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase)) normalized = normalized[11..];
            else if (normalized.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase)) normalized = normalized[7..];
            if (normalized.StartsWith("Authentication:", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("MCP_AUTH_", StringComparison.OrdinalIgnoreCase))
                start.Environment.Remove(key);
        }
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        if (environmentMode is not null) start.Environment["MCP_AUTH_MODE"] = environmentMode;

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the HTTP host.");
        using var cancellation = new CancellationTokenSource();
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var drains = Task.WhenAll(DrainAsync(process.StandardOutput, stdout, cancellation.Token),
            DrainAsync(process.StandardError, stderr, cancellation.Token));
        var failures = new List<string>();
        try
        {
            await process.WaitForExitAsync().WaitAsync(StartupTimeout);
        }
        catch (TimeoutException)
        {
            failures.Add("HTTP host did not reject startup within the deadline.");
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(CleanupTimeout);
                }
            }
            catch (Exception exception)
            {
                failures.Add("HTTP host termination failed: " + exception);
            }
            try
            {
                // Exit alone is insufficient: both streams must reach EOF, even after a forced kill.
                await drains.WaitAsync(CleanupTimeout);
            }
            catch (Exception exception)
            {
                failures.Add("HTTP host output drainage failed: " + exception);
            }
            finally
            {
                cancellation.Cancel();
                try { await drains.WaitAsync(CleanupTimeout); }
                catch (Exception exception) { failures.Add("HTTP host reader cleanup failed: " + exception); }
                lock (stdout) File.WriteAllText(Path.Combine(artifacts, "stdout.log"), stdout.ToString());
                lock (stderr) File.WriteAllText(Path.Combine(artifacts, "stderr.log"), stderr.ToString());
                File.WriteAllText(Path.Combine(artifacts, "exit.log"),
                    $"Exit code: {(process.HasExited ? process.ExitCode.ToString() : "not terminated")}\n" + string.Join('\n', failures));
            }
        }

        failures.Should().BeEmpty($"startup, termination and output drainage must complete; evidence: {artifacts}");
        var output = stdout + "\n" + stderr;
        HasExpectedFailure(process.ExitCode, output, diagnostic).Should().BeTrue(
            $"startup must exit nonzero with the exact authentication diagnostic and no disposal failure; " +
            $"exit: {process.ExitCode}; evidence: {artifacts}\n{output}");
    }

    private static async Task DrainAsync(StreamReader reader, StringBuilder capture, CancellationToken cancellationToken)
    {
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
            lock (capture) capture.Append(buffer, 0, count);
    }
}
