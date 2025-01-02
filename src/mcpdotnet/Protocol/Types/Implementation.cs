namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the name and version of an MCP implementation.
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