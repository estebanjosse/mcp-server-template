using System.Diagnostics;
using System.Globalization;

namespace McpServer.Template.Template.Tests;

internal sealed class OwnedProcess : IAsyncDisposable
{
    internal static bool SupportsProcessGroups => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();
    internal static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
    internal static string LauncherDll => Path.Combine(AppContext.BaseDirectory, "ProcessProbe.dll");

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);
    private readonly DirectoryInfo? control;
    private bool started;
    private Task? supervisorExit;
    private Task? stop;

    internal OwnedProcess(string executable, string[] arguments)
    {
        control = SupportsProcessGroups ? Directory.CreateTempSubdirectory("mcp-process-") : null;
        StartInfo = new ProcessStartInfo(control is null ? executable : "/bin/sh") { UseShellExecute = false };
        if (control is not null)
        {
            // The CLR duplicates its standard descriptors at startup. Give it /dev/null and pass the
            // target's descriptors separately, so even those CLR duplicates cannot delay protocol EOF.
            StartInfo.ArgumentList.Add("-c");
            StartInfo.ArgumentList.Add("exec 3<&0 4>&1 5>&2 && exec \"$@\" </dev/null >/dev/null 2>&1");
            StartInfo.ArgumentList.Add("owned-session");
            StartInfo.ArgumentList.Add(Dotnet);
            StartInfo.ArgumentList.Add(LauncherDll);
            StartInfo.ArgumentList.Add("supervise-session");
            StartInfo.ArgumentList.Add(control.FullName);
            StartInfo.ArgumentList.Add(executable);
        }
        foreach (var argument in arguments) StartInfo.ArgumentList.Add(argument);
        Process = new Process { StartInfo = StartInfo };
    }

    internal ProcessStartInfo StartInfo { get; }
    internal Process Process { get; }

    internal void Start()
    {
        if (!Process.Start()) throw new InvalidOperationException($"Failed to start {StartInfo.FileName}.");
        started = true;
        if (control is not null) supervisorExit = Process.WaitForExitAsync();
    }

    // Authorization is separate from starting the supervisor: without it, teardown cannot launch the target.
    internal async Task<int?> RunTargetAsync()
    {
        if (control is null)
        {
            await Process.WaitForExitAsync();
            return Process.ExitCode;
        }

        if (stop is null) File.WriteAllBytes(ControlFile("start"), []);
        var exited = supervisorExit!;
        var status = ControlFile("target-exit");
        while (!exited.IsCompleted && !File.Exists(status))
            await Task.WhenAny(exited, Task.Delay(PollInterval));
        if (File.Exists(status)) return int.Parse(File.ReadAllText(status), CultureInfo.InvariantCulture);
        await exited;
        AssertSupervisorStopped();
        return null; // Teardown interrupted a target that had not yet reported exit, or was never launched.
    }

    internal Task StopAsync() => stop ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        if (control is not null)
        {
            File.WriteAllBytes(ControlFile("stop"), []);
            // Never signal a PID/PGID from this process. The in-session supervisor acknowledges cleanup by
            // signalling its own group, including itself, while it still owns the group identity.
            await supervisorExit!.WaitAsync(TimeSpan.FromSeconds(5));
            AssertSupervisorStopped();
        }
        else if (!Process.HasExited)
        {
            // Best effort where this harness cannot own a POSIX process group.
            try { Process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) when (Process.HasExited) { }
        }
    }

    private void AssertSupervisorStopped()
    {
        const int killed = 128 + 9;
        var fault = ControlFile("fault");
        if (File.Exists(fault)) throw new InvalidOperationException(File.ReadAllText(fault));
        if (Process.ExitCode != killed || !File.Exists(ControlFile("stopping")))
            throw new InvalidOperationException($"Owned session supervisor exited without acknowledging cleanup (exit {Process.ExitCode}).");
    }

    private string ControlFile(string name) => Path.Combine(control!.FullName, name);

    // Read only after the supervisor's acknowledged stop.
    internal bool TargetWasLaunched => File.ReadAllText(ControlFile("stopping")) == "target launched";

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (started && stop is null) await StopAsync(); // Covers setup failures before the target is authorized.
        }
        finally
        {
            Process.Dispose();
            // Keep a pending stop visible even if the supervisor exceeded the cleanup deadline.
            if (control is not null && (!started || supervisorExit?.IsCompletedSuccessfully == true))
                control.Delete(recursive: true);
        }
    }
}
