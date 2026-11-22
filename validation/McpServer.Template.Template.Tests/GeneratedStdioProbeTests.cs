using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class GeneratedStdioProbeTests
{
    [PosixFact]
    public async Task Exited_leader_with_inherited_pipes_fails_qualification_and_terminates_descendant()
    {
        var root = Path.Combine(Path.GetTempPath(), "generated-stdio-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var lease = Path.Combine(root, "descendant.lock");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            bool operationsCompleted = false;
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => GeneratedStdioProbe.RunAsync(
                GeneratedStdioProbe.LauncherDll, root, root, "2026-07-28", false, async wire =>
                {
                    Assert.Empty((await wire.RequestAsync("ping", new { })).EnumerateObject());
                    operationsCompleted = true;
                }, "exit-before-descendant", lease));
            Assert.True(operationsCompleted, "The negative fixture must fail shutdown, not the protocol operation.");
            Assert.Contains(root, failure.Message);
            Assert.IsType<TimeoutException>(failure.InnerException);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), $"Cleanup exceeded its bound: {root}");
            Assert.Contains("notifications/message", File.ReadAllText(Path.Combine(root, "stdout.log")));
            Assert.Contains("Descendant holds both pipes after leader exit.", File.ReadAllText(Path.Combine(root, "stderr.log")));
            // Teardown can release the stdout/stderr descriptors just before the lock descriptor.
            var release = Stopwatch.StartNew();
            while (!CanOpenLease(lease) && release.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(20);
            Assert.True(CanOpenLease(lease), $"Owned descendant survived qualification failure: {root}");
        }
        finally
        {
            // Never leave the negative control's descendant alive when exercising a broken harness.
            if (File.Exists(lease + ".pid") && File.Exists(lease) && !CanOpenLease(lease))
            {
                var pid = int.Parse(File.ReadAllText(lease + ".pid"), CultureInfo.InvariantCulture);
                try
                {
                    using var child = Process.GetProcessById(pid);
                    child.Kill();
                }
                catch (ArgumentException) { } // Already reaped.
                catch (InvalidOperationException) { } // Already exited.
            }
        }
    }

    private static bool CanOpenLease(string path)
    {
        // Process existence can include a zombie; an exited process cannot still hold the lease.
        try
        {
            using var lease = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException) { return false; }
    }

    private sealed class PosixFactAttribute : FactAttribute
    {
        public PosixFactAttribute()
        {
            if (!GeneratedStdioProbe.SupportsProcessGroups) Skip = "Requires POSIX process groups.";
        }
    }
}