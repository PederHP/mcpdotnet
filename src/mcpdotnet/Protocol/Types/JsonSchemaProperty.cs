namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents a property in a JSON schema.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class JsonSchemaProperty
{
    /// <summary>
    /// The type of the property. Should be a JSON Schema type and is required.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the property.
    /// </summary>
    public string? Description { get; set; }
}
