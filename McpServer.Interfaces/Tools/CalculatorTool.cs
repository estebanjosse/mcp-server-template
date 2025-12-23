using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Abstractions;

namespace McpServer.Interfaces.Tools;

/// <summary>
/// A simple calculator tool that performs basic arithmetic operations.
/// </summary>
public class CalculatorTool : ITool
{
    public string Name => "calculator";

    public string Description => "Performs basic arithmetic operations (add, subtract, multiply, divide)";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            operation = new
            {
                type = "string",
                description = "The operation to perform",
                @enum = new[] { "add", "subtract", "multiply", "divide" }
            },
            a = new
            {
                type = "number",
                description = "The first operand"
            },
            b = new
            {
                type = "number",
                description = "The second operand"
            }
        },
        required = new[] { "operation", "a", "b" }
    };

    public Task<ToolResult> ExecuteAsync(IDictionary<string, object?>? arguments, CancellationToken cancellationToken = default)
    {
        if (arguments == null)
        {
            return Task.FromResult(new ToolResult("Error: arguments are required", isError: true));
        }

        if (!arguments.TryGetValue("operation", out var operationObj) || operationObj == null)
        {
            return Task.FromResult(new ToolResult("Error: operation parameter is required", isError: true));
        }

        if (!arguments.TryGetValue("a", out var aObj) || aObj == null)
        {
            return Task.FromResult(new ToolResult("Error: a parameter is required", isError: true));
        }

        if (!arguments.TryGetValue("b", out var bObj) || bObj == null)
        {
            return Task.FromResult(new ToolResult("Error: b parameter is required", isError: true));
        }

        var operation = operationObj.ToString()?.ToLowerInvariant();
        
        if (!TryConvertToDouble(aObj, out var a))
        {
            return Task.FromResult(new ToolResult("Error: parameter 'a' must be a number", isError: true));
        }

        if (!TryConvertToDouble(bObj, out var b))
        {
            return Task.FromResult(new ToolResult("Error: parameter 'b' must be a number", isError: true));
        }

        try
        {
            var result = operation switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" => b != 0 ? a / b : throw new DivideByZeroException("Cannot divide by zero"),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };

            return Task.FromResult(new ToolResult($"Result: {result}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult($"Error: {ex.Message}", isError: true));
        }
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        
        if (value == null)
        {
            return false;
        }

        if (value is double d)
        {
            result = d;
            return true;
        }

        if (value is int i)
        {
            result = i;
            return true;
        }

        if (value is long l)
        {
            result = l;
            return true;
        }

        if (value is float f)
        {
            result = f;
            return true;
        }

        return double.TryParse(value.ToString(), out result);
    }
}
