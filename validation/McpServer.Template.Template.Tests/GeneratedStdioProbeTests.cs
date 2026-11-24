using System.Diagnostics;

using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class GeneratedStdioProbeTests
{
    [PosixFact]
    public async Task Clean_stdio_shutdown_releases_descendant_lease_even_when_pipes_reach_eof()
    {
        var root = Path.Combine(Path.GetTempPath(), "generated-stdio-clean-eof-" + Guid.NewGuid().ToString("N"));
        var lease = Path.Combine(root, "descendant.lock");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await GeneratedStdioProbe.RunAsync(OwnedProcess.LauncherDll, root, root, "2026-07-28", false,
                async wire => Assert.Empty((await wire.RequestAsync("ping", new { })).EnumerateObject()),
                "exit-with-silent-descendant", lease);
            Assert.Contains("Descendant holds the lease without holding the host pipes.", File.ReadAllText(Path.Combine(root, "stderr.log")));
            Assert.True(File.Exists(lease + ".ready"), $"Descendant never acquired its lease: {root}");
            await DescendantLease.AssertReleasedAsync(lease, "successful EOF");
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15), $"Cleanup exceeded its bound: {root}");
        }
        finally { DescendantLease.RequestFixtureStop(lease); }
    }

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
                OwnedProcess.LauncherDll, root, root, "2026-07-28", false, async wire =>
                {
                    Assert.Empty((await wire.RequestAsync("ping", new { })).EnumerateObject());
                    operationsCompleted = true;
                }, "exit-before-descendant", lease));
            Assert.True(operationsCompleted, "The negative fixture must fail shutdown, not the protocol operation.");
            Assert.Contains(root, failure.Message);
            var timeout = Assert.IsType<TimeoutException>(failure.InnerException);
            Assert.Contains("exited with code 0, but its output pipes did not reach EOF", timeout.Message);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(20), $"Cleanup exceeded its bound: {root}");
            Assert.Contains("notifications/message", File.ReadAllText(Path.Combine(root, "stdout.log")));
            Assert.Contains("Descendant holds both output pipes.", File.ReadAllText(Path.Combine(root, "stderr.log")));
            await DescendantLease.AssertReleasedAsync(lease, "qualification failure");
        }
        finally { DescendantLease.RequestFixtureStop(lease); }
    }
}

internal static class DescendantLease
{
    internal static async Task AssertReleasedAsync(string lease, string after)
    {
        Assert.True(File.Exists(lease + ".ready"), $"Descendant never acquired its lease: {lease}");
        // Teardown can release the stdout/stderr descriptors just before the lock descriptor.
        var release = Stopwatch.StartNew();
        while (!CanOpenLease(lease) && release.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(20);
        Assert.True(CanOpenLease(lease), $"Owned descendant survived {after}: {lease}");
    }

    // Negative controls can ask a surviving fixture to stop without signalling any PID.
    internal static void RequestFixtureStop(string lease) => File.WriteAllBytes(lease + ".stop", []);

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
}

internal sealed class PosixFactAttribute : FactAttribute
{
    public PosixFactAttribute()
    {
        if (!OwnedProcess.SupportsProcessGroups) Skip = "Requires POSIX process groups.";
    }
}