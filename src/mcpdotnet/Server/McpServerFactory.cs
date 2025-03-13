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
    private readonly IServerTransport? _serverTransport;
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
        : this(Options.Create(options), serverDelegates ?? Options.Create<McpServerDelegates>(new()), loggerFactory, serviceProvider)
    {
        _serverTransport = serverTransport ?? throw new ArgumentNullException(nameof(serverTransport));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpServerFactory"/> class.
    /// </summary>
    /// <param name="options">Configuration options for this server, including capabilities. 
    /// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>
    /// <param name="serviceProvider">Optional service provider to create new instances.</param>
    /// <param name="loggerFactory">Logger factory to use for logging</param>
    /// <param name="serverDelegates"></param>
    public McpServerFactory(IOptions<McpServerOptions> options, IOptions<McpServerDelegates> serverDelegates, ILoggerFactory loggerFactory, IServiceProvider? serviceProvider = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _serverDelegates = serverDelegates?.Value;

        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a new server instance.
    /// 
    /// NB! You must register handlers for all supported capabilities on the server instance, before calling BeginListeningAsync.
    /// </summary>
    public IMcpServer CreateServer()
    {
        if (_serverTransport is null)
        {
            throw new InvalidOperationException($"{nameof(McpServerFactory)} must be constructed with an {nameof(IServerTransport)} to call this overload.");
        }

        return CreateServer(_serverTransport);
    }

    /// <summary>
    /// Creates a server instance using the provided transport. The transport must be connected to successfully create
    /// the server.
    /// </summary>
    /// <param name="transport">The transport is used to establish a connection for the server instance.</param>
    /// <returns>An instance of a server that utilizes the specified transport for communication.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the transport parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the transport is not is not already connected and not an <see cref="IServerTransport"/>.</exception>
    public IMcpServer CreateServer(ITransport transport)
    {
        if (transport is null)
        {
            throw new ArgumentNullException(nameof(transport));
        }
        if (transport is not IServerTransport && !transport.IsConnected)
        {
            throw new ArgumentException($"The transport must be connected", nameof(transport));
        }

        var server = new McpServer(transport, _options, _loggerFactory, _serviceProvider);
        _serverDelegates?.Apply(server);
        return server;
    }
}
