namespace McpServer.Template.Host.Http.Tests;

public sealed class HttpStartupTests
{
    private const string Diagnostic = "Invalid authentication mode 'unknown' from MCP_AUTH_MODE. Valid modes: none, simple, secure.";
    private const string Failure = "Unhandled exception. System.InvalidOperationException: " + Diagnostic;

    [Theory]
    [InlineData(0, Failure, false)]
    [InlineData(1, "", false)]
    [InlineData(1, "Unhandled exception. System.InvalidOperationException: unrelated failure", false)]
    [InlineData(1, "Unhandled exception. System.ObjectDisposedException: Cannot access a disposed object.", false)]
    [InlineData(1, Failure + " extra text", false)]
    [InlineData(1, Failure + "\nSystem.ObjectDisposedException: Cannot access a disposed object.", false)]
    [InlineData(1, Failure, true)]
    public void StartupVerdict_RequiresNonzeroExitAndExactAuthenticationDiagnostic(int exitCode, string output, bool expected)
    {
        HttpStartup.HasExpectedFailure(exitCode, output, Diagnostic).Should().Be(expected);
    }
}
