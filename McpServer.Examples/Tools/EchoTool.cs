using System;
using System.ComponentModel;
using McpServer.Abstractions;

namespace McpServer.Examples.Tools;

/// <summary>
/// A simple echo tool that returns the input message.
/// Demonstrates the simplicity of SimpleToolBase for trivial tools.
/// </summary>
public class EchoTool : SimpleToolBase
{
    public override string Name => "echo";

    public override string Description => "Echoes back the provided message";

    protected string Execute([Description("The message to echo back")] string message)
    {
        return $"Echo: {message}";
    }
}
