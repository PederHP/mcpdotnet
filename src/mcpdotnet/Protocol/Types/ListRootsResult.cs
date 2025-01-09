namespace McpDotNet.Protocol.Types;

/// <summary>
/// The client's response to a roots/list request from the server.
/// </summary>
public class ListRootsResult
{
    /// <summary>
    /// Additional metadata for the result. Reserved by the protocol for future use.
    /// </summary>
    public object? Meta { get; init; }

    /// <summary>
    /// The list of root URIs provided by the client.
    /// </summary>
    public required IReadOnlyList<Root> Roots { get; init; }
}
