
using McpDotNet.Protocol.Types;

namespace McpDotNet.Server;

/// <summary>
/// Configuration options for the MCP server. This is passed to the client during the initialization sequence, letting it know about the server's capabilities and
/// protocol version.
/// <see href="https://spec.modelcontextprotocol.io/specification/2024-11-05/basic/lifecycle/">See the protocol specification for details on capability negotiation</see>
/// </summary>
public record McpServerOptions
{
    /// <summary>
    /// Information about this server implementation.
    /// </summary>
    public required Implementation ServerInfo { get; init; }

    /// <summary>
    /// Server capabilities to advertise to the server.
    /// </summary>
    public ServerCapabilities? Capabilities { get; init; }

    /// <summary>
    /// Protocol version to request from the server.
    /// </summary>
    public string ProtocolVersion { get; init; } = "2024-11-05";

    /// <summary>
    /// Timeout for initialization sequence.
    /// </summary>
    public TimeSpan InitializationTimeout { get; init; } = TimeSpan.FromSeconds(60);
}
