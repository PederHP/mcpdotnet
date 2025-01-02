namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents the content of a tool response.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// There are multiple subtypes of content, depending on the "type" field, these are represented as separate classes.
/// </summary>
public class ToolContent
{
    /// <summary>
    /// The type of content. This determines the structure of the content object. Can be "image", "text", "resource".
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The base64-encoded image data.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// The MIME type of the image.
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// One of BlobResourceContents or TextResourceContents.
    /// </summary>
    public object? Resource { get; set; }

    /// <summary>
    /// Validates the content object. 
    /// </summary>
    /// <returns>Whether the type of tool content has the correct data fields provided.</returns>
    public bool Validate()
    {
        if (Type == "text")
        {
            return !string.IsNullOrEmpty(Text);
        }
        else if (Type == "image")
        {
            return !string.IsNullOrEmpty(Data) && !string.IsNullOrEmpty(MimeType);
        }
        else if (Type == "resource")
        {
            return Resource is BlobResourceContents || Resource is TextResourceContents;
        }
        else
        {
            return false;
        }
    }
}