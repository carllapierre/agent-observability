using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentCore.Utils;

/// <summary>
/// Generates OpenAI-compatible JSON schemas from C# types.
/// Produces strict schemas suitable for structured outputs.
/// </summary>
public static class JsonSchemaGenerator
{
    /// <summary>
    /// Generates a JSON schema string from type T.
    /// </summary>
    public static string Generate<T>() where T : class
    {
        return Generate(typeof(T));
    }

    /// <summary>
    /// Generates a JSON schema string from a type.
    /// </summary>
    public static string Generate(Type type)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = GenerateProperties(type),
            ["required"] = new JsonArray(GetRequiredProperties(type).Select(p => JsonValue.Create(p)).ToArray()),
            ["additionalProperties"] = false
        };

        return schema.ToJsonString();
    }

    private static JsonObject GenerateProperties(Type type)
    {
        var properties = new JsonObject();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            properties[ToCamelCase(prop.Name)] = GeneratePropertySchema(prop.PropertyType);
        }

        // Also check constructor parameters for record types
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor != null)
        {
            foreach (var param in constructor.GetParameters())
            {
                var camelName = ToCamelCase(param.Name ?? "");
                if (!properties.ContainsKey(camelName))
                {
                    properties[camelName] = GeneratePropertySchema(param.ParameterType);
                }
            }
        }

        return properties;
    }

    private static JsonObject GeneratePropertySchema(Type propertyType)
    {
        var schema = new JsonObject();

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType);
        if (underlyingType != null)
        {
            propertyType = underlyingType;
        }

        // Handle enums
        if (propertyType.IsEnum)
        {
            schema["type"] = "string";
            var enumValues = new JsonArray();
            foreach (var value in Enum.GetNames(propertyType))
            {
                enumValues.Add(value);
            }
            schema["enum"] = enumValues;
            return schema;
        }

        // Handle primitive types
        var jsonType = GetJsonType(propertyType);
        schema["type"] = jsonType;

        return schema;
    }

    private static string GetJsonType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => "integer",
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
            TypeCode.String or TypeCode.Char => "string",
            _ => "string"
        };
    }

    private static IEnumerable<string> GetRequiredProperties(Type type)
    {
        // For record types, get constructor parameters as required
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor != null)
        {
            foreach (var param in constructor.GetParameters())
            {
                if (param.Name != null)
                {
                    yield return ToCamelCase(param.Name);
                }
            }
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
