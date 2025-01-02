namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents a tool that the server is capable of calling. Part of the ListToolsResponse.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class Tool
{
    /// <summary>
    /// he name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// A JSON Schema object defining the expected parameters for the tool.
    /// </summary>
    public JsonSchema? InputSchema { get; set; }
}
