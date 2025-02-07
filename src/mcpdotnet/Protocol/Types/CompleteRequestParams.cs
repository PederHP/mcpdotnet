namespace McpDotNet.Protocol.Types;

/// <summary>
/// TODO
/// </summary>
public class CompleteRequestParams
{
    /// <summary>
    /// TODO
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("ref")]
    public required Reference Ref { get; init; }

    /// <summary>
    /// TODO
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("argument")]
    public required Argument Argument { get; init; }    
}
