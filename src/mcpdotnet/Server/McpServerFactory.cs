using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
public class McpServerFactory : IMcpServerFactory
{
    private readonly IServerTransport _serverTransport;
    private readonly McpServerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly McpServerDelegates? _serverDelegates;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerFactory"/> class.
    /// </summary>
    /// <param name="serverTransport">Transport to use for the server</param>
    /// <param name="options">Configuration options for this server, including capabilities. 
    /// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>
    /// <param name="serviceProvider">Optional service provider to create new instances.</param>
    /// <param name="loggerFactory">Logger factory to use for logging</param>
    /// <param name="serverDelegates"></param>
    public McpServerFactory(IServerTransport serverTransport, McpServerOptions options, ILoggerFactory loggerFactory, IOptions<McpServerDelegates>? serverDelegates = null, IServiceProvider? serviceProvider = null)
    {
        _serverTransport = serverTransport ?? throw new ArgumentNullException(nameof(serverTransport));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serverDelegates = serverDelegates?.Value;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a new server instance.
    /// 
    /// NB! You must register handlers for all supported capabilities on the server instance, before calling BeginListeningAsync.
    /// </summary>
    public IMcpServer CreateServer()
    {
        var server = new McpServer(_serverTransport, _options, _loggerFactory, _serviceProvider);

        _serverDelegates?.Apply(server);

        return server;
    }
}
