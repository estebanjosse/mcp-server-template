using System;
using System.ComponentModel;
using McpServer.Abstractions;

namespace McpServer.Examples.Tools;

/// <summary>
/// A simple calculator tool that performs basic arithmetic operations.
/// Demonstrates SimpleToolBase which automatically generates InputSchema from method parameters.
/// </summary>
public class CalculatorTool : SimpleToolBase
{
    public override string Name => "calculator";

    public override string Description => "Performs basic arithmetic operations (add, subtract, multiply, divide)";

    protected string Execute(
        [Description("Operation: add, subtract, multiply, divide")] string operation,
        [Description("First number")] double a,
        [Description("Second number")] double b)
    {
        return operation.ToLower() switch
        {
            "add" => $"Result: {a + b}",
            "subtract" => $"Result: {a - b}",
            "multiply" => $"Result: {a * b}",
            "divide" => b != 0 ? $"Result: {a / b}" : throw new DivideByZeroException("Cannot divide by zero"),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
    }
}
