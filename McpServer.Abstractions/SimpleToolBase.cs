using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Abstractions;

/// <summary>
/// Base class for tools that automatically generates InputSchema from method parameters.
/// Use this for simple tools. For complex tools with custom validation, implement ITool directly.
/// 
/// USAGE:
/// 1. Inherit from SimpleToolBase
/// 2. Override Name and Description properties
/// 3. Create a protected Execute method with your parameters
/// 4. Add [Description] attributes to parameters for better schema
/// 
/// EXAMPLE:
/// public class MyTool : SimpleToolBase
/// {
///     public override string Name => "my_tool";
///     public override string Description => "Does something useful";
///     
///     protected string Execute(
///         [Description("The input message")] string message,
///         [Description("Optional flag")] bool verbose = false)
///     {
///         return $"Result: {message}";
///     }
/// }
/// </summary>
public abstract class SimpleToolBase : ITool
{
    private readonly MethodInfo _executeMethod;
    private readonly Lazy<object> _inputSchema;

    public abstract string Name { get; }
    public abstract string Description { get; }

    protected SimpleToolBase()
    {
        // Find the Execute method with parameters
        _executeMethod = GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "Execute" && m.GetParameters().Length > 0)
            ?? throw new InvalidOperationException(
                $"{GetType().Name} must define a protected Execute method with at least one parameter. " +
                "Example: protected string Execute([Description(\"Input\")] string input) {{ ... }}");

        // Lazy generation of InputSchema
        _inputSchema = new Lazy<object>(() => GenerateInputSchema(_executeMethod));
    }

    public object InputSchema => _inputSchema.Value;

    public async Task<ToolResult> ExecuteAsync(IDictionary<string, object?>? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = _executeMethod.GetParameters();
            var args = new object?[parameters.Length];

            // Map arguments to method parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                
                // Handle CancellationToken parameter specially
                if (param.ParameterType == typeof(CancellationToken))
                {
                    args[i] = cancellationToken;
                    continue;
                }

                if (arguments?.TryGetValue(param.Name!, out var value) == true)
                {
                    args[i] = ConvertArgument(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    return new ToolResult($"Missing required parameter: {param.Name}", isError: true);
                }
            }

            // Invoke the Execute method
            var result = _executeMethod.Invoke(this, args);

            // Handle async/Task return types
            if (result is Task<string> taskString)
            {
                return new ToolResult(await taskString);
            }
            if (result is Task<ToolResult> taskResult)
            {
                return await taskResult;
            }
            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                var genericResult = resultProperty?.GetValue(task);
                return new ToolResult(genericResult?.ToString() ?? "null");
            }

            return new ToolResult(result?.ToString() ?? "null");
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap reflection exceptions
            return new ToolResult($"Error: {ex.InnerException.Message}", isError: true);
        }
        catch (Exception ex)
        {
            return new ToolResult($"Error: {ex.Message}", isError: true);
        }
    }

    private static object GenerateInputSchema(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Where(p => p.ParameterType != typeof(CancellationToken)) // Exclude CancellationToken
            .ToArray();

        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in parameters)
        {
            var descAttr = param.GetCustomAttribute<DescriptionAttribute>();
            var paramSchema = new Dictionary<string, object>
            {
                ["type"] = GetJsonSchemaType(param.ParameterType)
            };

            if (descAttr?.Description != null)
            {
                paramSchema["description"] = descAttr.Description;
            }

            // Handle enum types
            if (param.ParameterType.IsEnum)
            {
                paramSchema["enum"] = Enum.GetNames(param.ParameterType);
            }

            properties[param.Name!] = paramSchema;

            if (!param.HasDefaultValue)
            {
                required.Add(param.Name!);
            }
        }

        return new
        {
            type = "object",
            properties,
            required = required.ToArray()
        };
    }

    private static string GetJsonSchemaType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "string";
        
        if (underlyingType == typeof(int) || underlyingType == typeof(long) || 
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal) || underlyingType == typeof(short) ||
            underlyingType == typeof(byte))
            return "number";
        
        if (underlyingType == typeof(bool))
            return "boolean";
        
        if (underlyingType.IsEnum)
            return "string";

        return "string"; // Default fallback
    }

    private static object? ConvertArgument(object? value, Type targetType)
    {
        if (value == null)
            return null;

        if (targetType.IsInstanceOfType(value))
            return value;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            // Handle enums
            if (underlyingType.IsEnum && value is string stringValue)
            {
                return Enum.Parse(underlyingType, stringValue, ignoreCase: true);
            }

            // Handle numeric conversions from JsonElement
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return jsonElement.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => jsonElement.GetString(),
                    System.Text.Json.JsonValueKind.Number when underlyingType == typeof(int) => jsonElement.GetInt32(),
                    System.Text.Json.JsonValueKind.Number when underlyingType == typeof(long) => jsonElement.GetInt64(),
                    System.Text.Json.JsonValueKind.Number when underlyingType == typeof(double) => jsonElement.GetDouble(),
                    System.Text.Json.JsonValueKind.Number when underlyingType == typeof(float) => (float)jsonElement.GetDouble(),
                    System.Text.Json.JsonValueKind.Number when underlyingType == typeof(decimal) => jsonElement.GetDecimal(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    _ => Convert.ChangeType(jsonElement.ToString(), underlyingType)
                };
            }

            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            // Last resort: try to parse from string
            return value.ToString();
        }
    }
}
