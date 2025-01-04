using McpDotNet.Protocol.Messages;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// The server's response to a prompts/list request from the client.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class ListPromptsResult : PaginatedResult
{
    /// <summary>
    /// A list of prompts or prompt templates that the server offers.
    /// </summary>
    public List<Prompt> Prompts { get; set; } = new();
}