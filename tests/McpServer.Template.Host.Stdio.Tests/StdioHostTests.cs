using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests;

public abstract class StdioHostTestBase : IAsyncLifetime
{
    private protected StdioSession Host { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Host = new StdioSession(GetType().Name);
        await Host.InitializeAsync();
    }

    public Task DisposeAsync() => Host.DisposeAsync().AsTask();
}

public sealed class StdioHostTests : StdioHostTestBase
{

    [Fact]
    public void Initialize_NegotiatesRequestedProtocolAndServerIdentity()
    {
        Host.Initialization.GetProperty("protocolVersion").GetString().Should().Be("2025-11-25");
        var server = Host.Initialization.GetProperty("serverInfo");
        server.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        server.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        var capabilities = Host.Initialization.GetProperty("capabilities");
        capabilities.GetProperty("prompts").ValueKind.Should().Be(JsonValueKind.Object);
        capabilities.GetProperty("resources").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task InitializedNotification_AllowsSubsequentPing()
        => (await Host.RequestAsync("ping")).ValueKind.Should().Be(JsonValueKind.Object);

    [Fact]
    public async Task ToolsDiscovery_RespectsOptionalCapability()
    {
        if (Host.Initialization.GetProperty("capabilities").TryGetProperty("tools", out var capability))
        {
            capability.ValueKind.Should().Be(JsonValueKind.Object);
            var tools = (await Host.RequestAsync("tools/list")).GetProperty("tools").EnumerateArray().ToArray();
            tools.Select(t => t.GetProperty("name").GetString()).Should().OnlyHaveUniqueItems();
        }
        // No tools capability is valid for a template generated without sample tools.
    }

    [Fact]
    public async Task PromptsList_ExposesGreetingWithOptionalLanguage()
    {
        var prompts = (await Host.RequestAsync("prompts/list")).GetProperty("prompts").EnumerateArray().ToArray();
        prompts.Select(p => p.GetProperty("name").GetString()).Should().OnlyHaveUniqueItems();
        var greeting = prompts.Single(p => p.GetProperty("name").GetString() == "greeting");
        var language = greeting.GetProperty("arguments").EnumerateArray().Single(a => a.GetProperty("name").GetString() == "language");
        (language.TryGetProperty("required", out var required) && required.GetBoolean()).Should().BeFalse();
    }

    [Fact]
    public async Task GreetingPrompt_ReturnsExactFrenchMessage()
    {
        var result = await Host.RequestAsync("prompts/get", new { name = "greeting", arguments = new { language = "fr" } });
        var message = result.GetProperty("messages").EnumerateArray().Should().ContainSingle().Subject;
        message.GetProperty("role").GetString().Should().Be("user");
        message.GetProperty("content").GetProperty("type").GetString().Should().Be("text");
        message.GetProperty("content").GetProperty("text").GetString().Should().Be(
            "Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.");
    }

    [Fact]
    public async Task ResourcesList_ExposesUniqueWelcomeAndStatusWithMimeTypes()
    {
        var resources = (await Host.RequestAsync("resources/list")).GetProperty("resources").EnumerateArray().ToArray();
        resources.Select(r => r.GetProperty("uri").GetString()).Should().OnlyHaveUniqueItems();
        resources.Single(r => r.GetProperty("uri").GetString() == "resource://welcome").GetProperty("mimeType").GetString().Should().Be("text/plain");
        resources.Single(r => r.GetProperty("uri").GetString() == "resource://status").GetProperty("mimeType").GetString().Should().Be("application/json");
    }

    [Fact]
    public async Task WelcomeResource_ReturnsExactTemplateContent()
    {
        var text = await ReadResourceAsync("resource://welcome", "text/plain");
        text.Replace("\r\n", "\n").Should().Be($"""
            Welcome to McpServer.Template!

            This is a demonstration of a clean, scalable Model Context Protocol (MCP) server{" "}
            built with .NET 10 and proper architecture.

            Features:
            - Shared business logic between stdio and HTTP transports
            - Strict separation of concerns (Contracts / Application / Infrastructure / MCP Adapter / Hosts)
            - Easy addition of tools, prompts, and resources
            - Testable, idiomatic .NET code with DI and Options pattern

            Available Tools:
            - echo: Echoes back your message with a timestamp
            - calc_divide: Divides two numbers

            Available Prompts:
            - greeting: Generates a multilingual greeting message

            Available Resources:
            - resource://welcome: This welcome message
            - resource://status: Dynamic server status information
            """);
    }

    [Fact]
    public async Task StatusResource_ReturnsCurrentRunningStatus()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        using var document = JsonDocument.Parse(await ReadResourceAsync("resource://status", "application/json"));
        var status = document.RootElement;
        status.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("Timestamp", "Version", "Uptime", "Status");
        status.GetProperty("Status").GetString().Should().Be("Running");
        var timestamp = DateTimeOffset.ParseExact(status.GetProperty("Timestamp").GetString()!, "yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        timestamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTimeOffset.UtcNow);
        status.GetProperty("Version").GetString().Should().MatchRegex(@"^\d+\.\d+\.\d+\.\d+$");
        status.GetProperty("Uptime").GetString().Should().MatchRegex(@"^(\d+d \d+h \d+m|\d+h \d+m \d+s|\d+m \d+s|\d+s)$");
    }

    [Fact]
    public async Task StdinEof_ExitsZeroAndCompletesBothStreamDrains()
        => await Host.StopAsync();

    [Fact]
    public async Task Stdout_ContainsOnlyCompleteMcpFramesIncludingShutdown()
    {
        await Host.RequestAsync("prompts/list");
        await Host.StopAsync();
        Host.AssertPureStdout();
        Host.Stderr.Should().NotBeNullOrWhiteSpace("the real host must retain its normal console diagnostics on stderr");
    }

    private async Task<string> ReadResourceAsync(string uri, string mimeType)
    {
        var result = await Host.RequestAsync("resources/read", new { uri });
        var content = result.GetProperty("contents").EnumerateArray().Should().ContainSingle().Subject;
        content.GetProperty("uri").GetString().Should().Be(uri);
        content.GetProperty("mimeType").GetString().Should().Be(mimeType);
        return content.GetProperty("text").GetString()!;
    }
}
