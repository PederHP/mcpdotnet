namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents a JSON schema for a tool's input arguments.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class JsonSchema
{
    /// <summary>
    /// The type of the schema, should be "object".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Map of property names to property definitions.
    /// </summary>
    public Dictionary<string, JsonSchemaProperty>? Properties { get; set; }

    /// <summary>
    /// List of required property names.
    /// </summary>
    public List<string>? Required { get; set; }
}
