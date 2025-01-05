namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents the content of a resource.
/// </summary>
public class ResourceContents
{
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The type of content.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// The text content of the resource.
    /// </summary>
    public string? Text { get; set; }


    /// <summary>
    /// The base64-encoded binary content of the resource.
    /// </summary>
    public string? Blob { get; set; }
}