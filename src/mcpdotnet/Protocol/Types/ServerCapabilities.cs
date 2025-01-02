namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the capabilities that a server may support.
/// </summary>
public record ServerCapabilities
{
    /// <summary>
    /// Experimental, non-standard capabilities that the server supports.
    /// </summary>
    [JsonPropertyName("experimental")]
    public Dictionary<string, object>? Experimental { get; init; }

    /// <summary>
    /// Present if the server supports sending log messages to the client.
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingCapability? Logging { get; init; }

    /// <summary>
    /// Present if the server offers any prompt templates.
    /// </summary>
    [JsonPropertyName("prompts")]
    public PromptsCapability? Prompts { get; init; }

    /// <summary>
    /// Present if the server offers any resources to read.
    /// </summary>
    [JsonPropertyName("resources")]
    public ResourcesCapability? Resources { get; init; }

    /// <summary>
    /// Present if the server offers any tools to call.
    /// </summary>
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; init; }
}
