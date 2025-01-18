// Enable nullable reference types, as we need this for dynamic mapping of JSON schema to .NET types
#nullable enable

namespace SimpleToolsConsole;

public static class McpTypeMapper
{
    public static Type MapJsonToDotNetType(string jsonType, bool required)
    {
        // For simplicity, we don't handle complex types in this sample, but use object instead
        var baseType = jsonType switch
        {
            "string" => typeof(string),
            "integer" => typeof(int),
            "number" => typeof(double),
            "boolean" => typeof(bool),
            "array" => typeof(IEnumerable<object>),
            "object" => typeof(IDictionary<string, object>),
            _ => typeof(object)
        };

        // If it's a value type and not required, make it nullable
        if (!required && baseType.IsValueType)
        {
            return typeof(Nullable<>).MakeGenericType(baseType);
        }

        // Reference types are already nullable when not required
        return baseType;
    }
}
