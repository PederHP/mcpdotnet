using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// Describes the name and version of an MCP implementation.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public record Implementation
{
    /// <summary>
    /// Name of the implementation.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Version of the implementation.
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
}