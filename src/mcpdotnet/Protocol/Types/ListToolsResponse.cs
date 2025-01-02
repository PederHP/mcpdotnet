namespace McpDotNet.Protocol.Types;

/// <summary>
/// A response to a request to list the tools available on the server.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class ListToolsResponse
{
    /// <summary>
    /// The server's response to a tools/list request from the client.
    /// </summary>
    public List<Tool> Tools { get; set; } = new();
}
