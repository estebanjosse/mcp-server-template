using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

if (args[0] == "launch-in-session")
{
    SessionLauncher.Run(args[1..]);
    return;
}
if (args[0] == "exit-before-descendant")
{
    // Complete a request successfully, then exit only after the probe closes stdin.
    if (await Console.In.ReadLineAsync() is null) return;
    await Console.Out.WriteLineAsync("""{"jsonrpc":"2.0","id":1,"result":{}}""");
    if (await Console.In.ReadLineAsync() is not null) throw new InvalidOperationException("Expected stdin EOF.");
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
    foreach (var argument in new[] { Assembly.GetExecutingAssembly().Location, "hold-pipes", args[1],
        Environment.ProcessId.ToString(CultureInfo.InvariantCulture) })
        start.ArgumentList.Add(argument);
    using var descendant = Process.Start(start)!;
    File.WriteAllText(args[1] + ".pid", descendant.Id.ToString(CultureInfo.InvariantCulture));
    return;
}
if (args[0] == "hold-pipes")
{
    using var lease = File.Open(args[1], FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
    try
    {
        using var leader = Process.GetProcessById(int.Parse(args[2], CultureInfo.InvariantCulture));
        await leader.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(4));
    }
    catch (ArgumentException) { } // Leader already reaped.
    await Console.Out.WriteLineAsync("""{"jsonrpc":"2.0","method":"notifications/message","params":{}}""");
    await Console.Error.WriteLineAsync("Descendant holds both pipes after leader exit.");
    await Task.Delay(TimeSpan.FromSeconds(45)); // Self-clean if the regression test itself is interrupted.
    return;
}
if (args[0] == "child")
{
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}
var info = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
info.ArgumentList.Add("child");
using var child = Process.Start(info)!;
File.WriteAllText(args[1], child.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
await Task.WhenAll(Flood(Console.Out), Flood(Console.Error));

static async Task Flood(TextWriter writer)
{
    for (var i = 0; i < 256; i++) await writer.WriteLineAsync(new string('x', 4096));
    await writer.FlushAsync();
    await Task.Delay(Timeout.InfiniteTimeSpan);
}

// exec keeps the launched host as the session leader and preserves redirected streams.
internal static class SessionLauncher
{
    public static void Run(string[] arguments)
    {
        if (setsid() == -1) throw new Win32Exception(Marshal.GetLastPInvokeError(), "Cannot start an isolated stdio session.");
        var executable = Environment.ProcessPath!;
        string[] argv = [executable, .. arguments];
        var native = new IntPtr[argv.Length + 1]; // execv requires a trailing null.
        try
        {
            for (int i = 0; i < argv.Length; i++) native[i] = Marshal.StringToCoTaskMemUTF8(argv[i]);
            execv(native[0], native);
            throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Cannot exec {executable} in the stdio session.");
        }
        finally
        {
            foreach (var pointer in native) Marshal.FreeCoTaskMem(pointer);
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int setsid();

    [DllImport("libc", SetLastError = true)]
    private static extern int execv(IntPtr path, IntPtr[] argv);
}
