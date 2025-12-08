using System.Reflection;
using System.Text.Json;
using SimpleAgent.Core.ChatCompletion.Models;
using SimpleAgent.Core.Tools.Attributes;
using SimpleAgent.Core.Tools.Models;

namespace SimpleAgent.Core.Tools.Services;

/// <summary>
/// Registry for discovering and executing tools via reflection.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ToolInfo> _tools = new();

    public ToolRegistry(params Type[] toolTypes)
    {
        foreach (var type in toolTypes)
        {
            Register(type);
        }
    }

    private void Register(Type toolType)
    {
        var toolAttr = toolType.GetCustomAttribute<ToolAttribute>();
        if (toolAttr is null)
            throw new ArgumentException($"Type {toolType.Name} is not decorated with [Tool]");

        var executeMethod = toolType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"Tool {toolType.Name} must have a static Execute method");

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

        _tools[toolAttr.Name] = new ToolInfo(descriptor, executeMethod);
    }

    public IReadOnlyList<ToolDescriptor> GetDescriptors() => _tools.Values.Select(t => t.Descriptor).ToList();

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

        var result = method.Invoke(null, args);
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

    private record ToolInfo(ToolDescriptor Descriptor, MethodInfo Method);
}

