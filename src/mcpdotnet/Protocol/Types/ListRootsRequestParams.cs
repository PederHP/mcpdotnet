namespace McpDotNet.Protocol.Types;

/// <summary>
/// A request from the server to get a list of root URIs from the client.
/// </summary>
public class ListRootsRequestParams
{
    /// <summary>
    /// Optional progress token for out-of-band progress notifications.
    /// </summary>
    public string? ProgressToken { get; init; }
}
