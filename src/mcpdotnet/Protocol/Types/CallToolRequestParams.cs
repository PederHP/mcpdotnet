namespace McpDotNet.Protocol.Types;

/// <summary>
/// TODO
/// </summary>
public class CallToolRequestParams
{
    /// <summary>
    /// TODO
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// TODO
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("arguments")]
    public Dictionary<string,object>? Arguments { get; init; }    
}
