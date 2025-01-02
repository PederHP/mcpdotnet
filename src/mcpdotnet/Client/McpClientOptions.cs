using McpDotNet.Protocol.Types;

namespace McpDotNet.Client;

/// <summary>
/// Configuration options for the MCP client. This is passed to servers during the initialization sequence, letting them know about the client's capabilities and
/// protocol version.
/// <see href="https://spec.modelcontextprotocol.io/specification/basic/lifecycle/">See the protocol specification for details on capability negotiation</see>
/// </summary>
public record McpClientOptions
{
    /// <summary>
    /// Information about this client implementation.
    /// </summary>
    public required Implementation ClientInfo { get; init; }

    /// <summary>
    /// Client capabilities to advertise to the server.
    /// </summary>
    public ClientCapabilities? Capabilities { get; init; }

    /// <summary>
    /// Protocol version to request from the server.
    /// </summary>
    public string ProtocolVersion { get; init; } = "2024-11-05";

    /// <summary>
    /// Timeout for initialization sequence.
    /// </summary>
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromSeconds(60);
}
