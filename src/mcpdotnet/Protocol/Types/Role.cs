using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents the type of role in the conversation.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public enum Role
{
    /// <summary>
    /// Corresponds to the user in the conversation.
    /// </summary>
    [JsonPropertyName("user")]
    User,

    /// <summary>
    /// Corresponds to the AI in the conversation.
    /// </summary>
    [JsonPropertyName("assistant")]
    Assistant
}