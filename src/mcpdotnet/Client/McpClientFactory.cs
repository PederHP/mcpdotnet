namespace McpDotNet.Client;

using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;

/// <summary>
/// Factory for creating MCP clients based on configuration. It caches clients for reuse, so it is safe to call GetClientAsync multiple times.
/// Call GetClientAsync to get a client for a specific server (by ID), which will create a new client and connect if it doesn't already exist.
/// All server configurations must be passed in the constructor. 
/// Capabilities (as defined in client options) are shared across all clients, as the client host can always decide not to use certain capabilities.
/// </summary>
public class McpClientFactory
{
    private readonly Dictionary<string, McpServerConfig> _serverConfigs;
    private readonly McpClientOptions _clientOptions;
    private readonly Dictionary<string, IMcpClient> _clients = new();
    private readonly Func<McpServerConfig, IMcpTransport> _transportFactoryMethod;
    private readonly Func<IMcpTransport, McpClientOptions, IMcpClient> _clientFactoryMethod;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClientFactory"/> class.
    /// It is not necessary to pass factory methods for creating transports and clients, as default implementations are provided.
    /// Custom factory methods can be provided for mocking or to use custom transport or client implementations.
    /// </summary>
    /// <param name="serverConfigs">Configuration objects for each server the factory should support.</param>
    /// <param name="clientOptions">A configuration object which specifies client capabilities and protocol version.</param>
    /// <param name="transportFactoryMethod">An optional factory method which returns transport implementations based on a server configuration.</param>
    /// <param name="clientFactoryMethod">An optional factory method which creates a client based on client options and transport implementation. </param>
    public McpClientFactory(
        IEnumerable<McpServerConfig> serverConfigs,
        McpClientOptions clientOptions,
        Func<McpServerConfig, IMcpTransport>? transportFactoryMethod = null,
        Func<IMcpTransport, McpClientOptions, IMcpClient>? clientFactoryMethod = null)
    {
        _serverConfigs = serverConfigs.ToDictionary(c => c.Id);
        _clientOptions = clientOptions;
        _transportFactoryMethod = transportFactoryMethod ?? CreateTransport;
        _clientFactoryMethod = clientFactoryMethod ?? ((transport, options) => new McpClient(transport, options));
    }

    /// <summary>
    /// Gets or creates a client for the specified server. The first time a server is requested, a new client is created and connected.
    /// Note that this will often spawn the server process during connection, so in some cases you want to call this method only when needed.
    /// In other cases, you may want to call it ahead of time to ensure the server is ready when needed, as it may take some time to start up.
    /// </summary>
    /// <param name="serverId">The ID of the server to connect to. It must have been passed in the serverConfigs when constructing the factory.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<IMcpClient> GetClientAsync(string serverId, CancellationToken cancellationToken = default)
    {
        if (!_serverConfigs.TryGetValue(serverId, out var config))
        {
            throw new ArgumentException($"Server with ID '{serverId}' not found.", nameof(serverId));
        }

        if (_clients.TryGetValue(serverId, out var existingClient))
        {
            return existingClient;
        }

        var transport = _transportFactoryMethod(config);
        var client = _clientFactoryMethod(transport, _clientOptions);
        await client.ConnectAsync(cancellationToken);

        _clients[serverId] = client;
        return client;
    }

    private IMcpTransport CreateTransport(McpServerConfig config)
    {
        return config.TransportType.ToLowerInvariant() switch
        {
            "stdio" => new StdioTransport(new StdioTransportOptions
            {
                Command = GetCommand(config),
                Arguments = config.TransportOptions?.GetValueOrDefault("arguments")?.Split(' '),
                WorkingDirectory = config.TransportOptions?.GetValueOrDefault("workingDirectory"),
                EnvironmentVariables = config.TransportOptions?
                    .Where(kv => kv.Key.StartsWith("env:"))
                    .ToDictionary(kv => kv.Key.Substring(4), kv => kv.Value)
            }),
            // Add other transport types here
            _ => throw new ArgumentException($"Unsupported transport type '{config.TransportType}'.", nameof(config))
        };
    }

    private string GetCommand(McpServerConfig config)
    {
        if (config.TransportOptions == null)
        {
            return config.Location!;
        }

        var command = config.TransportOptions.GetValueOrDefault("command");
        if (string.IsNullOrEmpty(command))
        {
            return config.Location!;
        }

        if (config.TransportOptions.ContainsKey("arguments"))
        {
            var arguments = config.TransportOptions.GetValueOrDefault("arguments");
            config.TransportOptions["arguments"] = $"/c {command} {arguments}";
        }
        else
        {
            config.TransportOptions["arguments"] = $"/c {command}";
        }
        return $"cmd.exe";
    }
}