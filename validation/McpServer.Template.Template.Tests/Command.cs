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
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = cwd, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        start.Environment["DOTNET_NOLOGO"] = "1";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        // Repository/host overrides are opt-in for outer regression runs only.
        start.Environment.Remove("MCP_TEMPLATE_REPOSITORY");
        start.Environment.Remove("MCP_STDIO_HOST_DLL");
        start.Environment.Remove("MCP_STDIO_HOST_CWD");
        // Keep generated-host diagnostics with this command's external evidence.
        start.Environment["MCP_TEST_ARTIFACTS"] = Path.Combine(Path.GetDirectoryName(log)!, "host-diagnostics");
        File.WriteAllText(log + ".command.json", JsonSerializer.Serialize(new { executable, arguments, cwd, timeout }));
        using var process = new Process { StartInfo = start };
        await using var stdout = new FileStream(log + ".stdout.log", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await using var stderr = new FileStream(log + ".stderr.log", FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        process.Start();
        using var drains = new CancellationTokenSource();
        var output = process.StandardOutput.BaseStream.CopyToAsync(stdout, drains.Token);
        var error = process.StandardError.BaseStream.CopyToAsync(stderr, drains.Token);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(), output, error).WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            // Kill the owned tree, then bound both exit acknowledgement and inherited pipe drains.
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            drains.Cancel();
            try { await Task.WhenAll(process.WaitForExitAsync(), output, error).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception cleanup) when (cleanup is TimeoutException or OperationCanceledException or IOException) { }
            File.WriteAllText(log + ".timeout.txt", $"Timed out after {stopwatch.Elapsed}. Owned process tree cleanup requested.");
            throw new TimeoutException($"Command timed out, not a template rejection. Logs: {log}");
        }
        await stdout.FlushAsync();
        await stderr.FlushAsync();
        File.WriteAllText(log + ".result.json", JsonSerializer.Serialize(new { process.ExitCode, seconds = stopwatch.Elapsed.TotalSeconds }));
        // Streams are drained directly to disk; only bounded tails are included in assertion diagnostics.
        return new CommandResult(process.ExitCode, ReadTail(log + ".stdout.log") + "\n" + ReadTail(log + ".stderr.log"), log);
    }

    private static string ReadTail(string path)
    {
        using var stream = File.OpenRead(path);
        stream.Seek(Math.Max(0, stream.Length - 16384), SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
