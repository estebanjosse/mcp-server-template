using System.Xml.Linq;
using Xunit;

namespace McpServer.Template.Template.Tests;

internal static class TrxReports
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static void Verify(string directory, string[] expectedAssemblies)
    {
        Assert.NotEmpty(expectedAssemblies);
        var reports = Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories);
        Assert.Equal(expectedAssemblies.Length, reports.Length);
        var actual = new List<string>();
        foreach (var file in reports)
        {
            var document = XDocument.Load(file);
            var counters = Assert.Single(document.Descendants(Ns + "Counters"));
            int Count(string key) => int.Parse(counters.Attribute(key)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(Count("total") > 0, $"No tests executed in {file}");
            Assert.Equal(Count("total"), Count("executed"));
            Assert.Equal(Count("total"), Count("passed"));
            foreach (var key in new[] { "failed", "error", "timeout", "aborted", "notExecuted", "inconclusive", "disconnected", "warning", "completed", "inProgress", "pending", "passedButRunAborted", "notRunnable" })
                Assert.Equal(0, Count(key));
            Assert.Equal("Completed", Assert.Single(document.Descendants(Ns + "ResultSummary")).Attribute("outcome")!.Value);
            var definitions = document.Descendants(Ns + "TestDefinitions").Elements(Ns + "UnitTest").ToArray();
            var assemblies = definitions.Select(e => e.Attribute("storage")!.Value.Replace('\\', '/').Split('/').Last()).Distinct(StringComparer.OrdinalIgnoreCase);
            actual.Add(Assert.Single(assemblies));
            var results = document.Descendants(Ns + "UnitTestResult").ToArray();
            Assert.Equal(Count("total"), results.Length);
            Assert.All(results, r => Assert.Equal("Passed", r.Attribute("outcome")!.Value));
            Assert.Equal(results.Length, results.Select(r => r.Attribute("executionId")!.Value).Distinct().Count());
            var definedIds = definitions.Select(d => d.Attribute("id")!.Value).ToHashSet();
            Assert.All(results, r => Assert.Contains(r.Attribute("testId")!.Value, definedIds));
        }
        Assert.Equal(actual.Count, actual.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(expectedAssemblies.Select(a => a.ToLowerInvariant()).Order(), actual.Select(a => a.ToLowerInvariant()).Order());
    }
}
