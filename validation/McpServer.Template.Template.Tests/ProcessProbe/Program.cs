using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

if (args[0] == "supervise-session")
{
    await SessionSupervisor.RunAsync(args[1], args[2], args[3..]);
    return;
}
if (args[0] is "exit-before-descendant" or "command-exit-before-descendant" or "exit-with-silent-descendant"
    or "command-exit-with-silent-descendant")
{
    if (!args[0].StartsWith("command-", StringComparison.Ordinal))
    {
        // Complete a request successfully, then exit only after the probe closes stdin.
        if (await Console.In.ReadLineAsync() is null) return;
        await Console.Out.WriteLineAsync("""{"jsonrpc":"2.0","id":1,"result":{}}""");
        if (await Console.In.ReadLineAsync() is not null) throw new InvalidOperationException("Expected stdin EOF.");
    }
    var silent = args[0].EndsWith("silent-descendant", StringComparison.Ordinal);
    var start = new ProcessStartInfo(Environment.ProcessPath!)
    {
        UseShellExecute = false, RedirectStandardOutput = silent, RedirectStandardError = silent
    };
    foreach (var argument in new[] { Assembly.GetExecutingAssembly().Location, silent ? "hold-lease" : "hold-pipes", args[1] })
        start.ArgumentList.Add(argument);
    using var descendant = Process.Start(start)!;
    if (silent)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        while (!File.Exists(args[1] + ".ready")) await Task.Delay(20, deadline.Token);
        await Console.Error.WriteLineAsync("Descendant holds the lease without holding the host pipes.");
    }
    if (args[0] == "command-exit-with-silent-descendant")
        Environment.ExitCode = int.Parse(args[2], CultureInfo.InvariantCulture);
    return;
}
if (args[0] == "hold-lease")
{
    using var lease = File.Open(args[1], FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    using (File.Create(args[1] + ".ready")) { } // Signal only after acquiring the lease.
    await WaitForFixtureStopAsync(args[1]);
    return;
}
if (args[0] == "hold-pipes")
{
    using var lease = File.Open(args[1], FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    using (File.Create(args[1] + ".ready")) { }
    await Console.Out.WriteLineAsync("""{"jsonrpc":"2.0","method":"notifications/message","params":{}}""");
    await Console.Error.WriteLineAsync("Descendant holds both output pipes.");
    await WaitForFixtureStopAsync(args[1]);
    return;
}
if (args[0] == "child")
{
    using var lease = File.Open(args[1], FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    using (File.Create(args[1] + ".ready")) { }
    await WaitForFixtureStopAsync(args[1]);
    return;
}
var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
info.ArgumentList.Add("child");
info.ArgumentList.Add(args[1]);
using var child = Process.Start(info)!;
await Task.WhenAll(Flood(Console.Out), Flood(Console.Error));

static async Task Flood(TextWriter writer)
{
    for (var i = 0; i < 256; i++) await writer.WriteLineAsync(new string('x', 4096));
    await writer.FlushAsync();
    await Task.Delay(Timeout.InfiniteTimeSpan);
}

static async Task WaitForFixtureStopAsync(string lease)
{
    var deadline = Stopwatch.StartNew();
    while (!File.Exists(lease + ".stop") && deadline.Elapsed < TimeSpan.FromSeconds(45))
        await Task.Delay(20); // Self-clean if the regression test itself is interrupted.
}

internal static class SessionSupervisor
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(20);

    public static async Task RunAsync(string control, string executable, string[] arguments)
    {
        string ControlFile(string name) => Path.Combine(control, name);
        if (setsid() == -1)
        {
            var failure = new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot start an isolated process session.");
            File.WriteAllText(ControlFile("fault"), failure.ToString());
            return; // Never signal the ambient group if setsid failed.
        }
        var stop = ControlFile("stop");
        var launched = false;
        try
        {
            var start = ControlFile("start");
            while (!File.Exists(start) && !File.Exists(stop)) await Task.Delay(PollInterval);
            if (File.Exists(stop)) return;

            using var target = StartTarget(executable, arguments);
            launched = true;
            var exited = target.WaitForExitAsync();
            while (!exited.IsCompleted && !File.Exists(stop))
                await Task.WhenAny(exited, Task.Delay(PollInterval));
            if (exited.IsCompleted)
            {
                await exited;
                var status = ControlFile("target-exit.tmp");
                File.WriteAllText(status, target.ExitCode.ToString(CultureInfo.InvariantCulture));
                File.Move(status, ControlFile("target-exit"));
            }
            // Hold the group identity even after the target exits and all pipes reach EOF.
            while (!File.Exists(stop)) await Task.Delay(PollInterval);
        }
        catch (Exception failure)
        {
            File.WriteAllText(ControlFile("fault"), failure.ToString());
        }
        finally
        {
            try { File.WriteAllText(ControlFile("stopping"), launched ? "target launched" : "target not launched"); }
            finally
            {
                // Address the caller's own group, never a numeric PID/PGID that could have been reused.
                if (kill(0, 9) == -1)
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot terminate the owned process group.");
            }
        }
    }

    private static Process StartTarget(string executable, string[] arguments)
    {
        var info = new ProcessStartInfo("/bin/sh") { UseShellExecute = false };
        info.ArgumentList.Add("-c");
        // Restore the inherited descriptors before exec. No executable or argument is interpolated.
        info.ArgumentList.Add("exec <&3 >&4 2>&5 && exec 3<&- 4>&- 5>&- && exec \"$@\"");
        info.ArgumentList.Add("owned-target");
        info.ArgumentList.Add(executable);
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        try { return Process.Start(info) ?? throw new InvalidOperationException($"Failed to start {executable}."); }
        finally
        {
            // Drop the supervisor's own references so only the target tree can hold a pipe open.
            for (int descriptor = 3; descriptor <= 5; descriptor++) close(descriptor);
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int setsid();

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int signal);

    [DllImport("libc")]
    private static extern int close(int descriptor);
}
