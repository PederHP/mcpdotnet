namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents text-based resource contents.
/// </summary>
public record TextResourceContents : IResourceContents
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>
    /// The text content of the resource.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
