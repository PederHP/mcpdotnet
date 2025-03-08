namespace McpDotNet.Server;

/// <summary>
/// Factory for creating <see cref="IMcpServer"/> instances.
/// </summary>
public interface IMcpServerFactory
{
    /// <summary>
    ///  Creates a new server instance.
    /// </summary>
    /// <returns></returns>
    IMcpServer CreateServer();
}
