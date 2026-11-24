using FluentAssertions;
using ModelContextProtocol.Client;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests;

public sealed class StdioSdkClientTests
{
    [Theory]
    [InlineData(StdioSession.ModernVersion)]
    [InlineData("2025-11-25")]
    [InlineData("2025-06-18")]
    public async Task ListPrompts_WithPinnedSdkClientRevision_ExposesGreeting(string version)
    {
        var (command, dll, directory) = StdioSession.ResolveHost();
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = command,
            Arguments = [dll],
            WorkingDirectory = directory,
            ShutdownTimeout = TimeSpan.FromSeconds(5)
        });
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var client = await McpClient.CreateAsync(transport, new McpClientOptions { ProtocolVersion = version },
            cancellationToken: timeout.Token);

        var prompts = await client.ListPromptsAsync(cancellationToken: timeout.Token);
        prompts.Select(prompt => prompt.Name).Should().Contain("greeting");
    }
}
