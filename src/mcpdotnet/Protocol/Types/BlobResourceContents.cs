namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents binary resource contents.
/// </summary>
public record BlobResourceContents : IResourceContents
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>
    /// The base64-encoded binary content of the resource.
    /// </summary>
    [JsonPropertyName("blob")]
    public required string Blob { get; init; }
}

