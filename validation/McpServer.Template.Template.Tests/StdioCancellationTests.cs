using System.Collections.Concurrent;
using ModelContextProtocol.Client;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class StdioCancellationTests(TemplatePackage package) : IClassFixture<TemplatePackage>
{
    [Fact]
    public async Task CancelledTool_StopsServerWorkBeforeDisconnect_OnAllQualifiedRevisions()
    {
        var target = package.Target("stdio-cancellation");
        (await package.Generate(target, ["--host", "stdio"])).EnsureSuccess();
        // Add only a tool to the disposable consumer project. Keep the actual host entry
        // point, SDK transport, registration path, configuration and logging unchanged.
        var tools = Path.Combine(target, "src", "Generated.Server.Mcp", "Tools");
        Assert.DoesNotContain(package.PackageInventory, path => path.Contains("StdioCancellationTests", StringComparison.Ordinal));
        await File.WriteAllTextAsync(Path.Combine(tools, "CancellationProbe.cs"), """
            using ModelContextProtocol.Server;

            [McpServerToolType]
            public static class CancellationProbe
            {
                [McpServerTool(Name = "qualification_wait")]
                public static async Task WaitAsync(CancellationToken cancellationToken)
                {
                    Console.Error.WriteLine("qualification: entered handler");
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
                    finally
                    {
                        if (cancellationToken.IsCancellationRequested)
                            Console.Error.WriteLine("qualification: cancelled handler exited");
                    }
                }
            }
            """);
        var project = Path.Combine(target, "src", "Generated.Server.Host.Stdio", "Generated.Server.Host.Stdio.csproj");
        var output = Path.Combine(target, "host-output");
        (await package.Run(target, "publish-stdio-cancellation", "publish", project, "-c", "Release", "-o", output,
            "--disable-build-servers", "--maxcpucount:1", "-p:UseSharedCompilation=false")).EnsureSuccess();

        foreach (var revision in new[] { "2026-07-28", "2025-11-25", "2025-06-18" })
            await VerifyCancellationAsync(output, revision);
    }

    private async Task VerifyCancellationAsync(string output, string revision)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stderr = new ConcurrentQueue<string>();
        var diagnostics = Path.Combine(package.Artifacts, $"cancellation-{revision}.stderr.log");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            Arguments = [Path.Combine(output, "Generated.Server.Host.Stdio.dll")],
            WorkingDirectory = output,
            ShutdownTimeout = TimeSpan.FromSeconds(5),
            StandardErrorLines = line =>
            {
                stderr.Enqueue(line);
                if (line == "qualification: entered handler") started.TrySetResult();
                if (line == "qualification: cancelled handler exited") exited.TrySetResult();
            }
        });
        try
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var client = await McpClient.CreateAsync(transport, new McpClientOptions { ProtocolVersion = revision },
                cancellationToken: deadline.Token);
            using var cancel = new CancellationTokenSource();
            var invocation = client.CallToolAsync("qualification_wait", cancellationToken: cancel.Token).AsTask();
            try
            {
                await started.Task.WaitAsync(deadline.Token);
                Assert.False(invocation.IsCompleted);
                Assert.False(exited.Task.IsCompleted);
                cancel.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.WaitAsync(TimeSpan.FromSeconds(5)));
                // The local cancellation alone is insufficient. Require independent proof
                // from the server while its stdin is still open and it can handle requests.
                await exited.Task.WaitAsync(deadline.Token);
                Assert.Contains(await client.ListPromptsAsync(cancellationToken: deadline.Token), p => p.Name == "greeting");
            }
            finally
            {
                cancel.Cancel();
            }
        }
        finally
        {
            await File.WriteAllLinesAsync(diagnostics, stderr);
        }
    }
}
