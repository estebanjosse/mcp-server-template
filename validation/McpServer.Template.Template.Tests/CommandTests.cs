using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class CommandTests
{
    [Fact]
    public async Task Timeout_drains_both_streams_and_terminates_owned_descendants()
    {
        var root = Path.Combine(Path.GetTempPath(), "template-process-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var project = typeof(CommandTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "ProcessProbeProject").Value!;
        var output = Path.Combine(root, "probe");
        (await Command.Run("dotnet", ["build", project, "-c", "Release", "--output", output,
            "--artifacts-path", Path.Combine(root, "build"), "--disable-build-servers", "--maxcpucount:1",
            "-p:UseSharedCompilation=false"], root, Path.Combine(root, "build-probe"), TimeSpan.FromMinutes(2))).EnsureSuccess();
        var pidFile = Path.Combine(root, "child.pid");
        var log = Path.Combine(root, "timeout");
        var elapsed = Stopwatch.StartNew();
        await Assert.ThrowsAsync<TimeoutException>(() => Command.Run("dotnet",
            [Path.Combine(output, "ProcessProbe.dll"), "parent", pidFile], root, log, TimeSpan.FromSeconds(3)));
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(15), $"Cleanup took {elapsed.Elapsed}; logs: {root}");
        Assert.True(File.Exists(pidFile), $"Probe never spawned child; logs: {root}");
        var pid = int.Parse(File.ReadAllText(pidFile), System.Globalization.CultureInfo.InvariantCulture);
        try
        {
            using var child = Process.GetProcessById(pid);
            try { Assert.True(child.HasExited || await ExitedWithin(child), $"Owned descendant {pid} survived timeout; logs: {root}"); }
            finally
            {
                if (!child.HasExited)
                {
                    child.Kill(entireProcessTree: true);
                    await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
        }
        catch (ArgumentException) { } // Already reaped.
        Assert.True(new FileInfo(log + ".stdout.log").Length >= 1024 * 1024);
        Assert.True(new FileInfo(log + ".stderr.log").Length >= 1024 * 1024);
    }

    private static async Task<bool> ExitedWithin(Process process)
    {
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)); return true; }
        catch (TimeoutException) { return false; }
    }
}
