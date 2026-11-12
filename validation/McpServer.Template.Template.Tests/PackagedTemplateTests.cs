using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace McpServer.Template.Template.Tests;

public sealed class PackagedTemplateTests : IClassFixture<TemplatePackage>
{
    private readonly TemplatePackage package;

    public PackagedTemplateTests(TemplatePackage package, ITestOutputHelper output)
    {
        this.package = package;
        output.WriteLine($"Packaged-template evidence: {package.Artifacts}");
    }

    public static TheoryData<string, string[], string> Rejections => new()
    {
        { "omitted-host", [], "Missing mandatory option(s) for the template" },
        { "disabled-hosts", ["--http-host", "false", "--stdio-host", "false"], "Missing mandatory option(s) for the template" },
        { "invalid-false", ["--host", "false"], "Invalid option(s):" },
        { "invalid-none", ["--host", "none"], "Invalid option(s):" }
    };

    [Theory]
    [MemberData(nameof(Rejections))]
    public async Task Invalid_hosts_are_rejected_before_writing_files(string label, string[] flags, string diagnostic)
    {
        var target = package.Target(label);
        var result = await package.Generate(target, flags);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(diagnostic, result.Text);
        Assert.Contains("--host", result.Text);
        if (flags.Contains("--host"))
            Assert.Contains($"'{flags[^1]}' is not a valid value for --host.", result.Text);
        Assert.Empty(Directory.Exists(target) ? Directory.EnumerateFiles(target, "*", SearchOption.AllDirectories) : []);
    }

    public static TheoryData<string, string[], bool, bool> Selections => new()
    {
        { "http-explicit", ["--http-host", "true", "--stdio-host", "false"], true, false },
        { "stdio-explicit", ["--http-host", "false", "--stdio-host", "true"], false, true },
        { "both-explicit", ["--http-host", "true", "--stdio-host", "true"], true, true },
        { "http-bare", ["--http-host"], true, false },
        { "stdio-bare", ["--stdio-host"], false, true },
        { "both-bare", ["--http-host", "--stdio-host"], true, true },
        { "http-choice", ["--host", "http"], true, false },
        { "stdio-choice", ["--host", "stdio"], false, true },
        { "both-choice", ["--host", "both"], true, true },
        { "false-with-choice", ["--http-host", "false", "--stdio-host", "false", "--host", "http"], true, false },
        { "additive-choice", ["--http-host", "--host", "stdio"], true, true }
    };

    [Theory]
    [MemberData(nameof(Selections))]
    public async Task Legal_host_selections_generate_exact_solution_membership(string label, string[] flags, bool http, bool stdio)
    {
        var target = package.Target(label);
        (await package.Generate(target, [.. flags, "--include-tests", "true", "--include-sample-tools", "true"])).EnsureSuccess();
        VerifyInventory(target, http, stdio, tests: true, samples: true);
    }

    public static IEnumerable<object[]> Matrix =>
        from host in new[] { "http", "stdio", "both" }
        from tests in new[] { false, true }
        from samples in new[] { false, true }
        select new object[] { host, tests, samples };

    [Theory]
    [MemberData(nameof(Matrix))]
    public async Task Generated_matrix_builds_and_executes_every_test_assembly(string host, bool tests, bool samples)
    {
        var target = package.Target($"matrix-{host}-tests{tests}-samples{samples}");
        (await package.Generate(target, ["--host", host, "--include-tests", tests.ToString().ToLowerInvariant(),
            "--include-sample-tools", samples.ToString().ToLowerInvariant()])).EnsureSuccess();
        var projects = VerifyInventory(target, host != "stdio", host != "http", tests, samples);
        var solution = Assert.Single(Directory.GetFiles(target, "*.slnx"));
        (await package.Run(target, "build", "build", solution, "-c", "Release", "--disable-build-servers",
            "--maxcpucount:1", "-p:UseSharedCompilation=false")).EnsureSuccess();
        var expected = projects.Where(IsTestProject).ToArray();
        Assert.Equal(tests, expected.Length > 0);
        if (!tests) return;
        var reports = Path.Combine(target, "test-results");
        foreach (var project in expected)
        {
            var name = Path.GetFileNameWithoutExtension(project);
            (await package.Run(target, $"test-{name}", "test", project, "-c", "Release", "--no-build", "--no-restore",
                "--logger", $"trx;LogFileName={name}.trx", "--results-directory", reports,
                "--disable-build-servers", "--maxcpucount:1")).EnsureSuccess();
        }
        TrxReports.Verify(reports, expected.Select(p => Path.GetFileNameWithoutExtension(p) + ".dll").ToArray());
    }

    private static bool IsTestProject(string path) => XDocument.Load(path).Descendants("IsTestProject")
        .Any(e => string.Equals(e.Value, "true", StringComparison.OrdinalIgnoreCase));

    private string[] VerifyInventory(string target, bool http, bool stdio, bool tests, bool samples)
    {
        const string name = "Generated.Server";
        var projects = Directory.GetFiles(target, "*.csproj", SearchOption.AllDirectories);
        var expected = new List<string> { ".Application", ".Contracts", ".Infrastructure", ".Mcp" };
        if (http) expected.Add(".Host.Http");
        if (stdio) expected.Add(".Host.Stdio");
        if (tests)
        {
            expected.AddRange([".Application.Tests", ".Infrastructure.Tests", ".Mcp.Tests"]);
            if (http) expected.Add(".Host.Http.Tests");
            if (stdio) expected.Add(".Host.Stdio.Tests");
        }
        Assert.Equal(expected.Select(s => name + s).Order(), projects.Select(Path.GetFileNameWithoutExtension).Order());
        var solution = Assert.Single(Directory.GetFiles(target, "*.slnx"));
        var references = XDocument.Load(solution).Descendants("Project")
            .Select(e => Path.GetFullPath(Path.Combine(target, e.Attribute("Path")!.Value.Replace('\\', Path.DirectorySeparatorChar)))).ToArray();
        Assert.Equal(references.Length, references.Distinct().Count());
        Assert.Equal(projects.Order(), references.Order());
        Assert.All(references, p => Assert.True(File.Exists(p), p));
        Assert.False(Directory.Exists(Path.Combine(target, "validation")), "Repository-only validators must not be generated.");
        foreach (var tool in new[] { "EchoTool.cs", "CalcDivideTool.cs" })
            Assert.Equal(samples, File.Exists(Path.Combine(target, "src", name + ".Mcp", "Tools", tool)));
        Assert.Equal(samples && tests, Directory.Exists(Path.Combine(target, "tests", name + ".Mcp.Tests", "Tools")));
        return projects;
    }
}
