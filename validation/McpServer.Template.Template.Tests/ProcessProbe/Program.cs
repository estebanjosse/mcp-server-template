using System.Diagnostics;
using System.Reflection;

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
