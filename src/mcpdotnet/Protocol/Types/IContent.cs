// Protocol/Types/Content.cs
namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the type of role in the conversation.
/// </summary>
public enum Role
{
    [JsonPropertyName("user")]
    User,

    [JsonPropertyName("assistant")]
    Assistant
}

/// <summary>
/// Base interface for content types in messages.
/// </summary>
public interface IContent
{
    string Type { get; }
    Annotations? Annotations { get; }
}

/// <summary>
/// Represents text content in messages.
/// </summary>
public record TextContent : IContent
{
    /// <summary>
    /// The type of content. Always "text".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "text";

    /// <summary>
    /// The text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// Optional annotations for the content.
    /// </summary>
    [JsonPropertyName("annotations")]
    public Annotations? Annotations { get; init; }
}

/// <summary>
/// Represents image content in messages.
/// </summary>
public record ImageContent : IContent
{
    /// <summary>
    /// The type of content. Always "image".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "image";

    /// <summary>
    /// The base64-encoded image data.
    /// </summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>
    /// The MIME type of the image.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>
    /// Optional annotations for the content.
    /// </summary>
    [JsonPropertyName("annotations")]
    public Annotations? Annotations { get; init; }
}

/// <summary>
/// Represents annotations that can be attached to content.
/// </summary>
public record Annotations
{
    /// <summary>
    /// Describes who the intended customer of this object or data is.
    /// </summary>
    [JsonPropertyName("audience")]
    public Role[]? Audience { get; init; }

    /// <summary>
    /// Describes how important this data is for operating the server (0 to 1).
    /// </summary>
    [JsonPropertyName("priority")]
    public float? Priority { get; init; }
}
