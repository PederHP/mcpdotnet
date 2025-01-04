namespace McpDotNet.Protocol.Types;

/// <summary>
/// The server's response to a resources/read request from the client.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class ReadResourceResult
{
    /// <summary>
    /// A list of ResourceContents that this resource contains.
    /// </summary>
    public List<ResourceContents> Contents { get; set; } = new();
}
