using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed record CommandResult(int ExitCode, string Text, string Log)
{
    public void EnsureSuccess() => Assert.True(ExitCode == 0, $"Exit {ExitCode}; logs: {Log}\n{Text}");
}

internal static class Command
{
    public static async Task<CommandResult> Run(string executable, string[] arguments, string cwd, string log, TimeSpan timeout)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(log)!);
        await using var owned = new OwnedProcess(executable, arguments);
        var start = owned.StartInfo;
        start.WorkingDirectory = cwd;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        // Template rejection assertions use English CLI diagnostics, regardless of the runner's locale.
        start.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        // Repository/host overrides are opt-in for outer regression runs only.
        start.Environment.Remove("MCP_TEMPLATE_REPOSITORY");
        start.Environment.Remove("MCP_STDIO_HOST_DLL");
        start.Environment.Remove("MCP_STDIO_HOST_CWD");
        // Keep generated-host diagnostics with this command's external evidence.
        start.Environment["MCP_TEST_ARTIFACTS"] = Path.Combine(Path.GetDirectoryName(log)!, "host-diagnostics");
        File.WriteAllText(log + ".command.json", JsonSerializer.Serialize(new { executable, arguments, cwd, timeout }));
        var process = owned.Process;
        await using var stdout = new FileStream(log + ".stdout.log", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await using var stderr = new FileStream(log + ".stderr.log", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        owned.Start();
        using var drains = new CancellationTokenSource();
        var output = process.StandardOutput.BaseStream.CopyToAsync(stdout, drains.Token);
        var error = process.StandardError.BaseStream.CopyToAsync(stderr, drains.Token);
        var targetExit = owned.RunTargetAsync();
        var completed = Task.WhenAll(targetExit, output, error);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await completed.WaitAsync(timeout);
        }
        catch (Exception commandFailure)
        {
            var exitAtFailure = targetExit.IsCompletedSuccessfully ? targetExit.Result : null;
            var stopped = owned.StopAsync();
            Exception? cleanupFailure = null;
            // Preserve trailing evidence by waiting for real EOF before cancelling stuck drains.
            try { await completed.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception failure)
            {
                if (!ReferenceEquals(failure, commandFailure)) cleanupFailure = failure;
                if (!completed.IsCompleted)
                {
                    drains.Cancel();
                    try { await completed.WaitAsync(TimeSpan.FromSeconds(5)); }
                    catch (OperationCanceledException) when (drains.IsCancellationRequested) { }
                    catch (Exception drainFailure)
                    {
                        cleanupFailure = cleanupFailure is null ? drainFailure : new AggregateException(cleanupFailure, drainFailure);
                    }
                }
            }
            try { await stopped; }
            catch (Exception failure) { cleanupFailure = cleanupFailure is null ? failure : new AggregateException(cleanupFailure, failure); }
            if (commandFailure is TimeoutException)
            {
                File.WriteAllText(log + ".timeout.txt", $"Timed out after {stopwatch.Elapsed}. Target exit before cleanup: {exitAtFailure?.ToString() ?? "not observed"}. Owned process cleanup requested.\n{cleanupFailure}");
                throw new TimeoutException($"Command timed out, not a template rejection. Logs: {log}", cleanupFailure);
            }
            if (cleanupFailure is not null)
                throw new InvalidOperationException($"Command and cleanup failed. Logs: {log}", new AggregateException(commandFailure, cleanupFailure));
            throw;
        }
        await owned.StopAsync();
        int exitCode = await targetExit ?? throw new InvalidOperationException($"Command did not report its exit code. Logs: {log}");
        await stdout.FlushAsync();
        await stderr.FlushAsync();
        File.WriteAllText(log + ".result.json", JsonSerializer.Serialize(new { ExitCode = exitCode, seconds = stopwatch.Elapsed.TotalSeconds }));
        // Streams are drained directly to disk; only bounded tails are included in assertion diagnostics.
        return new CommandResult(exitCode, ReadTail(log + ".stdout.log") + "\n" + ReadTail(log + ".stderr.log"), log);
    }

    private static string ReadTail(string path)
    {
        using var stream = File.OpenRead(path);
        stream.Seek(Math.Max(0, stream.Length - 16384), SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
