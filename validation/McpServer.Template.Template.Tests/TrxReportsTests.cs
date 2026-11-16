using System.Xml.Linq;
using Xunit;
using Xunit.Sdk;

namespace McpServer.Template.Template.Tests;

public sealed class TrxReportsTests
{
    [Fact]
    public void Complete_nonempty_report_is_accepted() => WithReport((directory, _) => TrxReports.Verify(directory, ["Sample.Tests.dll"]));

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("wrong-assembly")]
    [InlineData("zero")]
    [InlineData("skipped")]
    [InlineData("failed")]
    [InlineData("missing-result")]
    [InlineData("duplicate-result")]
    [InlineData("aborted")]
    public void Incomplete_or_unsuccessful_reports_are_rejected(string defect) => WithReport((directory, document) =>
    {
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var path = Path.Combine(directory, "sample.trx");
        var counters = document.Descendants(ns + "Counters").Single();
        switch (defect)
        {
            case "missing": File.Delete(path); break;
            case "duplicate": File.Copy(path, Path.Combine(directory, "duplicate.trx")); break;
            case "wrong-assembly": document.Descendants(ns + "UnitTest").Single().SetAttributeValue("storage", "Other.Tests.dll"); break;
            case "zero": counters.SetAttributeValue("total", "0"); break;
            case "skipped": counters.SetAttributeValue("notExecuted", "1"); break;
            case "failed": counters.SetAttributeValue("failed", "1"); break;
            case "missing-result": document.Descendants(ns + "UnitTestResult").Single().Remove(); break;
            case "duplicate-result": document.Descendants(ns + "Results").Single().Add(new XElement(document.Descendants(ns + "UnitTestResult").Single())); break;
            case "aborted": document.Descendants(ns + "ResultSummary").Single().SetAttributeValue("outcome", "Aborted"); break;
        }
        if (defect is not ("missing" or "duplicate")) document.Save(path);
        Assert.ThrowsAny<XunitException>(() => TrxReports.Verify(directory, ["Sample.Tests.dll"]));
    });

    private static void WithReport(Action<string, XDocument> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "template-trx-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var document = XDocument.Parse("""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results><UnitTestResult testId="test-1" executionId="execution-1" outcome="Passed" /></Results>
              <TestDefinitions><UnitTest id="test-1" storage="/path/Sample.Tests.dll" /></TestDefinitions>
              <ResultSummary outcome="Completed"><Counters total="1" executed="1" passed="1" failed="0"
                error="0" timeout="0" aborted="0" notExecuted="0" inconclusive="0" disconnected="0"
                warning="0" completed="0" inProgress="0" pending="0" passedButRunAborted="0" notRunnable="0" /></ResultSummary>
            </TestRun>
            """);
        document.Save(Path.Combine(directory, "sample.trx"));
        try { action(directory, document); }
        finally { Directory.Delete(directory, recursive: true); } // Only this test's unique fixture.
    }
}
