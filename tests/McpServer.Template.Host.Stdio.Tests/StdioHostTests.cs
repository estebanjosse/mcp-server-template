using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace McpServer.Template.Host.Stdio.Tests;

public abstract class StdioHostTestBase : IAsyncLifetime
{
    private protected StdioSession Host { get; private set; } = null!;

    protected abstract string ProtocolVersion { get; }

    public async Task InitializeAsync()
    {
        Host = new StdioSession(GetType().Name);
        if (ProtocolVersion != StdioSession.ModernVersion) await Host.InitializeAsync(ProtocolVersion);
    }

    private protected Task<JsonElement> CallAsync(string method, object? parameters = null)
        => Host.RequestCapabilityAsync(ProtocolVersion, method, parameters);

    public Task DisposeAsync() => Host.DisposeAsync().AsTask();
}

public abstract class StdioHostTests : StdioHostTestBase
{
    [Fact]
    public async Task ResourceRead_WhenUriIsUnknown_ReturnsRevisionSpecificError()
    {
        const string uri = "resource://missing-fixture";
        var error = await Host.RequestCapabilityErrorAsync(ProtocolVersion, "resources/read", new { uri });
        error.GetProperty("code").GetInt32().Should().Be(ProtocolVersion == StdioSession.ModernVersion ? -32602 : -32002);
        error.GetProperty("message").GetString().Should().Be($"Unknown resource URI: '{uri}'");
        error.TryGetProperty("data", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Negotiation_ReportsExactRevisionAndAdvertisedCapabilities()
    {
        var handshake = ProtocolVersion == StdioSession.ModernVersion ? await CallAsync("server/discover") : Host.Initialization;
        if (ProtocolVersion == StdioSession.ModernVersion)
            handshake.GetProperty("supportedVersions").EnumerateArray().Select(v => v.GetString()).Should().Contain(ProtocolVersion);
        else
        {
            handshake.GetProperty("protocolVersion").GetString().Should().Be(ProtocolVersion);
            (await Host.RequestAsync("ping")).EnumerateObject().Should().BeEmpty();
        }

        var server = ProtocolVersion == StdioSession.ModernVersion
            ? handshake.GetProperty("_meta").GetProperty("io.modelcontextprotocol/serverInfo")
            : handshake.GetProperty("serverInfo");
        server.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        server.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        var capabilities = handshake.GetProperty("capabilities");
        capabilities.GetProperty("prompts").ValueKind.Should().Be(JsonValueKind.Object);
        capabilities.GetProperty("resources").ValueKind.Should().Be(JsonValueKind.Object);

        // dotnet new substitutes the flag; the repository itself includes samples.
        var samplesExpected = !string.Equals("__MCP_EXPECT_SAMPLE_TOOLS__", "false", StringComparison.OrdinalIgnoreCase);
        capabilities.TryGetProperty("tools", out var tools).Should().Be(samplesExpected);
        if (samplesExpected)
        {
            tools.ValueKind.Should().Be(JsonValueKind.Object);
            var list = (await CallAsync("tools/list")).GetProperty("tools").EnumerateArray();
            list.Select(t => t.GetProperty("name").GetString()).Should().BeEquivalentTo("echo", "calc_divide");
        }
    }

    [Fact]
    public async Task PromptsList_ExposesGreetingWithOptionalLanguage()
    {
        var prompts = (await CallAsync("prompts/list")).GetProperty("prompts").EnumerateArray().ToArray();
        prompts.Select(p => p.GetProperty("name").GetString()).Should().OnlyHaveUniqueItems();
        var greeting = prompts.Single(p => p.GetProperty("name").GetString() == "greeting");
        var language = greeting.GetProperty("arguments").EnumerateArray().Single(a => a.GetProperty("name").GetString() == "language");
        (language.TryGetProperty("required", out var required) && required.GetBoolean()).Should().BeFalse();
    }

    [Fact]
    public async Task GreetingPrompt_ReturnsExactFrenchMessage()
    {
        var result = await CallAsync("prompts/get", new { name = "greeting", arguments = new { language = "fr" } });
        var message = result.GetProperty("messages").EnumerateArray().Should().ContainSingle().Subject;
        message.GetProperty("role").GetString().Should().Be("user");
        message.GetProperty("content").GetProperty("type").GetString().Should().Be("text");
        message.GetProperty("content").GetProperty("text").GetString().Should().Be(
            "Bonjour ! Bienvenue sur le modèle de serveur MCP.\n\nYou can use the available tools to interact with the server.");
    }

    [Fact]
    public async Task ResourcesList_ExposesUniqueWelcomeAndStatusWithMimeTypes()
    {
        var resources = (await CallAsync("resources/list")).GetProperty("resources").EnumerateArray().ToArray();
        resources.Select(r => r.GetProperty("uri").GetString()).Should().OnlyHaveUniqueItems();
        resources.Single(r => r.GetProperty("uri").GetString() == "resource://welcome").GetProperty("mimeType").GetString().Should().Be("text/plain");
        resources.Single(r => r.GetProperty("uri").GetString() == "resource://status").GetProperty("mimeType").GetString().Should().Be("application/json");
    }

    [Fact]
    public async Task WelcomeResource_ReturnsExactTemplateContent()
    {
        var text = await ReadResourceAsync("resource://welcome", "text/plain");
        text.ReplaceLineEndings("\n").Should().Be($"""
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
            """.ReplaceLineEndings("\n"));
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
    {
        await CallAsync("prompts/list");
        await Host.StopAsync();
    }

    [Fact]
    public async Task Stdout_ContainsOnlyCompleteMcpFramesIncludingShutdown()
    {
        await CallAsync("prompts/list");
        await Host.StopAsync();
        Host.AssertPureStdout();
        Host.Stderr.Should().NotBeNullOrWhiteSpace("the real host must retain its normal console diagnostics on stderr");
    }

    private async Task<string> ReadResourceAsync(string uri, string mimeType)
    {
        var result = await CallAsync("resources/read", new { uri });
        if (ProtocolVersion == StdioSession.ModernVersion)
        {
            result.GetProperty("ttlMs").GetInt32().Should().Be(0);
            result.GetProperty("cacheScope").GetString().Should().Be("private");
        }
        else
        {
            result.TryGetProperty("ttlMs", out _).Should().BeFalse();
            result.TryGetProperty("cacheScope", out _).Should().BeFalse();
        }
        var content = result.GetProperty("contents").EnumerateArray().Should().ContainSingle().Subject;
        content.GetProperty("uri").GetString().Should().Be(uri);
        content.GetProperty("mimeType").GetString().Should().Be(mimeType);
        return content.GetProperty("text").GetString()!;
    }
}

public sealed class ModernStdioHostTests : StdioHostTests
{
    protected override string ProtocolVersion => StdioSession.ModernVersion;
}

public sealed class NovemberStdioHostTests : StdioHostTests
{
    protected override string ProtocolVersion => "2025-11-25";
}

public sealed class JuneStdioHostTests : StdioHostTests
{
    protected override string ProtocolVersion => "2025-06-18";
}
