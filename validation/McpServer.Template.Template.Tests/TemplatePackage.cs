using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace McpServer.Template.Template.Tests;

public sealed class TemplatePackage : IAsyncLifetime
{
    public string Repository { get; private set; } = "";
    public string Artifacts { get; private set; } = "";
    public string[] PackageInventory { get; private set; } = [];
    private string Hive => Path.Combine(Artifacts, "hive");

    public async Task InitializeAsync()
    {
        var compiledRoot = typeof(TemplatePackage).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "TemplateRepository").Value!;
        Repository = Path.GetFullPath(Environment.GetEnvironmentVariable("MCP_TEMPLATE_REPOSITORY") ?? compiledRoot);
        foreach (var marker in new[] { "McpServer.Template.csproj", ".template.config/template.json", "Directory.Build.props", "McpServer.Template.slnx" })
            if (!File.Exists(Path.Combine(Repository, marker))) throw new InvalidOperationException($"Invalid template repository: missing {marker} in {Repository}");
        using (var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repository, ".template.config/template.json"))))
            Assert.Equal("EasyMcp.McpServer.Template", config.RootElement.GetProperty("identity").GetString());
        var parent = Path.GetFullPath(Environment.GetEnvironmentVariable("MCP_TEST_ARTIFACTS") ?? Path.GetTempPath());
        foreach (var root in new[] { Repository, compiledRoot })
            if (Within(parent, root)) throw new InvalidOperationException("MCP_TEST_ARTIFACTS must be outside the repository.");
        Artifacts = Path.Combine(parent, "template-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Artifacts); // Unique owned directory; evidence is never deleted.
        File.WriteAllText(Path.Combine(Artifacts, "repository.txt"), Repository);
        (await Run(Repository, "pack", "pack", Path.Combine(Repository, "McpServer.Template.csproj"), "-c", "Release",
            "--output", Path.Combine(Artifacts, "packages"), "--artifacts-path", Path.Combine(Artifacts, "pack-artifacts"),
            "-p:Version=0.0.0-validation", "--disable-build-servers", "--maxcpucount:1")).EnsureSuccess();
        var package = Assert.Single(Directory.GetFiles(Path.Combine(Artifacts, "packages"), "*.nupkg"));
        File.WriteAllText(Path.Combine(Artifacts, "package.sha256"), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(package))));
        using (var archive = ZipFile.OpenRead(package)) PackageInventory = archive.Entries.Select(e => e.FullName).Order().ToArray();
        File.WriteAllText(Path.Combine(Artifacts, "package-inventory.json"), JsonSerializer.Serialize(PackageInventory));
        Assert.DoesNotContain(PackageInventory, p => p.StartsWith("content/validation/", StringComparison.Ordinal));
        (await Run(Repository, "install", "new", "install", package, "--debug:custom-hive", Hive)).EnsureSuccess();
    }

    private static bool Within(string path, string root)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(root), path);
        return relative == "." || (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }

    public string Target(string label)
    {
        var target = Path.Combine(Artifacts, label);
        Assert.False(Directory.Exists(target), $"Refusing to reuse output {target}");
        return target;
    }

    public Task<CommandResult> Generate(string target, string[] flags) => Run(Repository, Path.GetFileName(target) + "-generate",
        ["new", "mcp-server", "--name", "Generated.Server", "--output", target, "--debug:custom-hive", Hive, .. flags]);

    public Task<CommandResult> Run(string cwd, string label, params string[] arguments) =>
        Command.Run("dotnet", arguments, cwd, Path.Combine(Artifacts, "logs", label + "-" + Guid.NewGuid().ToString("N")), TimeSpan.FromMinutes(4));

    public Task DisposeAsync() => Task.CompletedTask;
}
