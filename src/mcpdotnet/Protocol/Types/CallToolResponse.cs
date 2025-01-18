namespace McpDotNet.Protocol.Types;

/// <summary>
/// A response to a request to call a tool on the server.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class CallToolResponse
{
    /// <summary>
    /// The server's response to a tools/call request from the client.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public List<Content> Content { get; set; } = new();

    /// <summary>
    /// Whether the tool call was unsuccessful. If true, the call was unsuccessful.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isError")]
    public bool IsError { get; set; }
}
