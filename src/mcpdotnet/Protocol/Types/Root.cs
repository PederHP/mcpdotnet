namespace McpDotNet.Protocol.Types;

/// <summary>
/// Represents a root URI and its metadata.
/// </summary>
public class Root
{
    /// <summary>
    /// The URI of the root.
    /// </summary>
    public required string Uri { get; init; }

    /// <summary>
    /// A human-readable name for the root.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Additional metadata for the root. Reserved by the protocol for future use.
    /// </summary>
    public object? Meta { get; init; }
}