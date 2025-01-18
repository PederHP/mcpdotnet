
using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;
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
