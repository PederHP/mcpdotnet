using McpDotNet.Configuration;
using McpDotNet.Logging;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Client;
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
    private readonly Func<IMcpTransport, McpServerConfig, McpClientOptions, IMcpClient> _clientFactoryMethod;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpClientFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClientFactory"/> class.
    /// It is not necessary to pass factory methods for creating transports and clients, as default implementations are provided.
    /// Custom factory methods can be provided for mocking or to use custom transport or client implementations.
    /// </summary>
    /// <param name="serverConfigs">Configuration objects for each server the factory should support.</param>
    /// <param name="clientOptions">A configuration object which specifies client capabilities and protocol version.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers for clients.</param>
    /// <param name="transportFactoryMethod">An optional factory method which returns transport implementations based on a server configuration.</param>
    /// <param name="clientFactoryMethod">An optional factory method which creates a client based on client options and transport implementation. </param>
    public McpClientFactory(
        IEnumerable<McpServerConfig> serverConfigs,
        McpClientOptions clientOptions,
        ILoggerFactory loggerFactory,
        Func<McpServerConfig, IMcpTransport>? transportFactoryMethod = null,
        Func<IMcpTransport, McpServerConfig, McpClientOptions, IMcpClient>? clientFactoryMethod = null)
    {
        _serverConfigs = serverConfigs.ToDictionary(c => c.Id);
        _clientOptions = clientOptions;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpClientFactory>();
        _transportFactoryMethod = transportFactoryMethod ?? CreateTransport;
        _clientFactoryMethod = clientFactoryMethod ?? ((transport, serverConfig, options) => new McpClient(transport, options, serverConfig, loggerFactory));

        // Initialize commands for stdio transport, this is to run commands in a shell even if specified directly, as otherwise
        //  the stdio protocol will not work correctly.
        _logger.InitializingStdioCommands();
        foreach (var config in _serverConfigs.Values)
        {
            if (config.TransportType.ToLowerInvariant() == "stdio")
            {
                InitializeCommand(config);
            }
        }
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
            _logger.ServerNotFound(serverId);
            throw new ArgumentException($"Server with ID '{serverId}' not found.", nameof(serverId));
        }

        if (_clients.TryGetValue(serverId, out var existingClient))
        {
            _logger.ClientExists(serverId, config.Name);
            return existingClient;
        }

        _logger.CreatingClient(serverId, config.Name);

        var transport = _transportFactoryMethod(config);
        var client = _clientFactoryMethod(transport, config, _clientOptions);
        await client.ConnectAsync(cancellationToken);

        _logger.ClientCreated(serverId, config.Name);

        _clients[serverId] = client;
        return client;
    }

    private IMcpTransport CreateTransport(McpServerConfig config)
    {
        var options = string.Join(", ", config.TransportOptions?.Select(kv => $"{kv.Key}={kv.Value}") ?? Enumerable.Empty<string>());
        _logger.CreatingTransport(config.Id, config.Name, config.TransportType, options);
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
            }, config, _loggerFactory),
            _ => throw new ArgumentException($"Unsupported transport type '{config.TransportType}'.", nameof(config))
        };
    }

    private string GetCommand(McpServerConfig config)
    {
        if (config.TransportOptions == null ||
            string.IsNullOrEmpty(config.TransportOptions.GetValueOrDefault("command")))
        {
            return config.Location!;
        }

        return $"cmd.exe";
    }

    /// <summary>
    /// Initializes a non-shell command by injecting a /c {command} argument, as the command will be run in a shell.
    /// </summary>
    private void InitializeCommand(McpServerConfig config)
    {
        // If the command is empty or already contains cmd.exe, we don't need to do anything
        var command = config.TransportOptions?.GetValueOrDefault("command");
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (command.Contains("cmd.exe"))
        {
            _logger.SkippingShellWrapper(config.Id, config.Name);
            return;
        }

        // If the command is not empty and does not contain cmd.exe, we need to inject /c {command}
        if (config.TransportOptions != null && !string.IsNullOrEmpty(command))
        {
            _logger.PromotingCommandToShellArgumentForStdio(config.Id, config.Name, command, config.TransportOptions.GetValueOrDefault("arguments") ?? "");
            config.TransportOptions["arguments"] = config.TransportOptions.ContainsKey("arguments")
                ? $"/c {command} {config.TransportOptions["arguments"]}"
                : $"/c {command}";
        }
    }
}