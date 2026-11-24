using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class CommandTests
{
    [PosixFact]
    public async Task Teardown_before_startup_authorization_never_launches_the_target()
    {
        await using var owned = new OwnedProcess("/bin/true", []);
        owned.StartInfo.RedirectStandardOutput = true;
        owned.StartInfo.RedirectStandardError = true;
        owned.Start();
        var stdout = owned.Process.StandardOutput.ReadToEndAsync();
        var stderr = owned.Process.StandardError.ReadToEndAsync();

        // No timing assumptions: withhold authorization even if the supervisor has already reached its gate.
        await owned.StopAsync();
        Assert.False(owned.TargetWasLaunched);
        Assert.Null(await owned.RunTargetAsync());
        await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(await stdout);
        Assert.Empty(await stderr);
    }

    [PosixFact]
    public async Task Completed_command_preserves_target_exit_and_releases_silent_descendant()
    {
        foreach (var exitCode in new[] { 0, 23 })
        {
            var root = Path.Combine(Path.GetTempPath(), "template-command-clean-exit-" + Guid.NewGuid().ToString("N"));
            var lease = Path.Combine(root, "descendant.lock");
            try
            {
                var result = await Command.Run(OwnedProcess.Dotnet,
                    [OwnedProcess.LauncherDll, "command-exit-with-silent-descendant", lease, exitCode.ToString(CultureInfo.InvariantCulture)],
                    root, Path.Combine(root, "command"), TimeSpan.FromSeconds(10));
                Assert.Equal(exitCode, result.ExitCode);
                Assert.Contains("Descendant holds the lease without holding the host pipes.", result.Text);
                await DescendantLease.AssertReleasedAsync(lease, "command completion");
            }
            finally { DescendantLease.RequestFixtureStop(lease); }
        }
    }

    [PosixFact]
    public async Task Timeout_releases_descendant_lease_even_after_command_leader_exits()
    {
        var root = Path.Combine(Path.GetTempPath(), "template-command-cleanup-" + Guid.NewGuid().ToString("N"));
        var lease = Path.Combine(root, "descendant.lock");
        var log = Path.Combine(root, "timeout");
        var elapsed = Stopwatch.StartNew();
        try
        {
            var failure = await Assert.ThrowsAsync<TimeoutException>(() => Command.Run(OwnedProcess.Dotnet,
                [OwnedProcess.LauncherDll, "command-exit-before-descendant", lease], root, log, TimeSpan.FromSeconds(5)));
            Assert.Contains(log, failure.Message);
            Assert.Null(failure.InnerException); // Cleanup must complete without the drain cancellation fallback.
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(20), $"Cleanup took {elapsed.Elapsed}; logs: {root}");
            Assert.Contains("notifications/message", File.ReadAllText(log + ".stdout.log"));
            Assert.Contains("Descendant holds both output pipes.", File.ReadAllText(log + ".stderr.log"));
            Assert.Contains("Target exit before cleanup: 0.", File.ReadAllText(log + ".timeout.txt"));
            await DescendantLease.AssertReleasedAsync(lease, "command timeout");
        }
        finally { DescendantLease.RequestFixtureStop(lease); }
    }

    [Fact]
    public async Task Timeout_drains_both_streams_and_terminates_owned_descendants()
    {
        var root = Path.Combine(Path.GetTempPath(), "template-process-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var lease = Path.Combine(root, "child.lock");
        var log = Path.Combine(root, "timeout");
        var elapsed = Stopwatch.StartNew();
        try
        {
            await Assert.ThrowsAsync<TimeoutException>(() => Command.Run(OwnedProcess.Dotnet,
                [OwnedProcess.LauncherDll, "parent", lease], root, log, TimeSpan.FromSeconds(3)));
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(15), $"Cleanup took {elapsed.Elapsed}; logs: {root}");
            await DescendantLease.AssertReleasedAsync(lease, "command timeout with a live target");
            Assert.True(new FileInfo(log + ".stdout.log").Length >= 1024 * 1024);
            Assert.True(new FileInfo(log + ".stderr.log").Length >= 1024 * 1024);
        }
        finally { DescendantLease.RequestFixtureStop(lease); }
    }
}
