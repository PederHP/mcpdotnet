using Microsoft.Extensions.Logging;
using McpDotNet.Protocol.Transport;

namespace McpDotNet.Server;

/// <summary>
/// Factory for creating <see cref="IMcpServer"/> instances.
/// This is the main entry point for creating a server.
/// Pass the server transport, options, and logger factory to the constructor. Server instructions are optional.
/// 
/// Then call CreateServer to create a new server instance.
/// You can create multiple servers with the same factory, but the transport must be able to handle multiple connections.
/// 
/// You must register handlers for all supported capabilities on the server instance, before calling BeginListeningAsync.
/// </summary>
public class McpServerFactory
{
    private readonly IServerTransport _serverTransport;
    private readonly McpServerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string? _serverInstructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerFactory"/> class.
    /// </summary>
    /// <param name="serverTransport">Transport to use for the server</param>
    /// <param name="options">Configuration options for this server, including capabilities. 
    /// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>
    /// <param name="serverInstructions">Optional server instructions to send to clients</param>
    /// <param name="loggerFactory">Logger factory to use for logging</param>
    public McpServerFactory(IServerTransport serverTransport, McpServerOptions options, ILoggerFactory loggerFactory,
        string? serverInstructions = null)
    {
        _serverTransport = serverTransport;
        _options = options;
        _loggerFactory = loggerFactory;
        _serverInstructions = serverInstructions;
    }

    /// <summary>
    /// Creates a new server instance.
    /// 
    /// NB! You must register handlers for all supported capabilities on the server instance, before calling BeginListeningAsync.
    /// </summary>
    public IMcpServer CreateServer()
    {
        return new McpServer(_serverTransport, _options, _serverInstructions, _loggerFactory);
    }
}
