using McpDotNet.Protocol.Transport;

namespace McpDotNet.Server;

/// <summary>
/// Factory for creating <see cref="IMcpServer"/> instances.
/// </summary>
public interface IMcpServerFactory
{
    /// <summary>
    /// Creates a new server instance.
    /// </summary>
    /// <returns>Returns an instance of an <see cref="IMcpServer"/> that can handle requests.</returns>
    IMcpServer CreateServer();

    /// <summary>
    /// Creates a server instance.
    /// </summary>
    /// <param name="transport">The transport mechanism used for server communication.</param>
    /// <returns>Returns an instance of an <see cref="IMcpServer"/> that can handle requests.</returns>
    IMcpServer CreateServer(ITransport transport);
}
