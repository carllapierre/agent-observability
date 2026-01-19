using System.Reflection;
using System.Text;
using System.Text.Json;
using AgentCore.ChatCompletion.Models;
using AgentCore.Tools.Attributes;
using AgentCore.Tools.Models;

namespace AgentCore.Tools.Services;

/// <summary>
/// Registry for discovering and executing tools via reflection.
/// Supports both static tools (types) and instance tools (objects).
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ToolInfo> _tools = new();

    /// <summary>
    /// Creates a registry from tool instances.
    /// </summary>
    public ToolRegistry(params object[] toolInstances)
    {
        foreach (var instance in toolInstances)
        {
            Register(instance);
        }
    }

    private void Register(object toolInstance)
    {
        var toolType = toolInstance.GetType();
        var toolAttr = toolType.GetCustomAttribute<ToolAttribute>();
        if (toolAttr is null)
            throw new ArgumentException($"Type {toolType.Name} is not decorated with [Tool]");

        // Try instance method first, then static
        var executeMethod = toolType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance)
            ?? toolType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"Tool {toolType.Name} must have an Execute method");

        var parameters = executeMethod.GetParameters()
            .Select(p => new ToolParameterDescriptor(
                Name: p.Name ?? "param",
                Type: GetJsonType(p.ParameterType),
                Description: p.GetCustomAttribute<ToolParameterAttribute>()?.Description ?? string.Empty,
                IsRequired: !p.HasDefaultValue,
                DefaultValue: p.HasDefaultValue ? p.DefaultValue : null
            ))
            .ToList();

        var descriptor = new ToolDescriptor(
            Name: toolAttr.Name,
            Description: toolAttr.Description,
            Parameters: parameters
        );

        _tools[toolAttr.Name] = new ToolInfo(descriptor, executeMethod, toolInstance);
    }

    public IReadOnlyList<ToolDescriptor> GetDescriptors() => _tools.Values.Select(t => t.Descriptor).ToList();

    /// <summary>
    /// Formats all registered tools as human-readable text for use in prompts.
    /// </summary>
    public string FormatAsText()
    {
        var sb = new StringBuilder();
        foreach (var tool in _tools.Values.Select(t => t.Descriptor))
        {
            sb.AppendLine($"- **{tool.Name}**: {tool.Description}");
            foreach (var param in tool.Parameters)
            {
                var required = param.IsRequired ? "(required)" : "(optional)";
                sb.AppendLine($"  - {param.Name} ({param.Type}) {required}: {param.Description}");
            }
        }
        return sb.ToString();
    }

    public string Execute(ToolCall toolCall)
    {
        if (!_tools.TryGetValue(toolCall.Name, out var toolInfo))
            throw new ArgumentException($"Unknown tool: {toolCall.Name}");

        var method = toolInfo.Method;
        var parameters = method.GetParameters();
        var args = new object?[parameters.Length];

        // Parse arguments from JSON
        var argsDict = string.IsNullOrEmpty(toolCall.Arguments)
            ? new Dictionary<string, JsonElement>()
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.Arguments)
              ?? new Dictionary<string, JsonElement>();

        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var paramName = param.Name ?? "param";

            if (argsDict.TryGetValue(paramName, out var value))
            {
                args[i] = ConvertJsonElement(value, param.ParameterType);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required parameter: {paramName}");
            }

        }

        // Invoke on instance (or null for static methods)
        var instance = method.IsStatic ? null : toolInfo.Instance;
        var result = method.Invoke(instance, args);
        return result?.ToString() ?? string.Empty;
    }

    private static string GetJsonType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Int16 => "integer",
            TypeCode.Double or TypeCode.Single or TypeCode.Decimal => "number",
            TypeCode.Boolean => "boolean",
            TypeCode.String => "string",
            _ => "string"
        };
    }

    private static object? ConvertJsonElement(JsonElement element, Type targetType)
    {
        return Type.GetTypeCode(targetType) switch
        {
            TypeCode.Int32 => element.GetInt32(),
            TypeCode.Int64 => element.GetInt64(),
            TypeCode.Double => element.GetDouble(),
            TypeCode.Boolean => element.GetBoolean(),
            TypeCode.String => element.GetString(),
            _ => element.GetString()
        };
    }

    private record ToolInfo(ToolDescriptor Descriptor, MethodInfo Method, object Instance);
}

