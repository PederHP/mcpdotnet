using System.Globalization;
using System.Runtime.InteropServices;
using McpDotNet.Configuration;
using McpDotNet.Logging;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Client;
/// <summary>
/// Factory for creating MCP clients based on configuration. It caches clients for reuse, so it is safe to call GetClientAsync multiple times.
/// Call GetClientAsync to get a client for a specific server (by ID), which will create a new client and connect if it doesn't already exist.
/// All server configurations must be passed in the constructor. 
/// Capabilities (as defined in client options) are shared across all clients, as the client host can always decide not to use certain capabilities.
/// </summary>
public class McpClientFactory : IDisposable
{
    private const string ARGUMENTS_OPTIONS_KEY = "arguments";
    private const string COMMAND_OPTIONS_KEY = "command";
    private readonly Dictionary<string, McpServerConfig> _serverConfigs;
    private readonly McpClientOptions _clientOptions;
    private readonly Dictionary<string, IMcpClient> _clients = [];
    private readonly Func<McpServerConfig, IClientTransport> _transportFactoryMethod;
    private readonly Func<IClientTransport, McpServerConfig, McpClientOptions, IMcpClient> _clientFactoryMethod;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpClientFactory> _logger;
    private bool _isDisposed;

    /// <summary>
    /// Gets or sets a value indicating whether clients should be disposed when the factory is disposed.
    /// </summary>
    public bool DisposeClientsOnDispose { get; set; } = true;

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
        ILoggerFactory? loggerFactory = null,
        Func<McpServerConfig, IClientTransport>? transportFactoryMethod = null,
        Func<IClientTransport, McpServerConfig, McpClientOptions, IMcpClient>? clientFactoryMethod = null)
    {
        if (serverConfigs is null)
        {
            throw new ArgumentNullException(nameof(serverConfigs));
        }

        if (clientOptions is null)
        {
            throw new ArgumentNullException(nameof(clientOptions));
        }

        loggerFactory ??= NullLoggerFactory.Instance;

        _serverConfigs = serverConfigs.ToDictionary(c => c.Id);
        _clientOptions = clientOptions;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<McpClientFactory>();
        _transportFactoryMethod = transportFactoryMethod ?? CreateTransport;
        _clientFactoryMethod = clientFactoryMethod ?? ((transport, serverConfig, options) => new McpClient(transport, options, serverConfig, loggerFactory));

        // Initialize commands for stdio transport, this is to run commands in a shell even if specified directly, as otherwise
        //  the stdio protocol will not work correctly.
        _logger.InitializingStdioCommands();
        foreach (var config in _serverConfigs.Values.Where(c => c.TransportType.Equals(TransportTypes.StdIo, StringComparison.OrdinalIgnoreCase)))
        {
            InitializeCommand(config);
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

        string endpointName = $"Client ({serverId}: {config.Name})";

        if (_clients.TryGetValue(serverId, out var existingClient))
        {
            _logger.ClientExists(endpointName);
            return existingClient;
        }

        _logger.CreatingClient(endpointName);

        var transport = _transportFactoryMethod(config);
        var client = _clientFactoryMethod(transport, config, _clientOptions);
        await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

        _logger.ClientCreated(endpointName);

        _clients[serverId] = client;
        return client;
    }

    internal Func<McpServerConfig, IClientTransport> TransportFactoryMethod => _transportFactoryMethod;

    private IClientTransport CreateTransport(McpServerConfig config)
    {
        string endpointName = $"Client ({config.Id}: {config.Name})";

        var options = string.Join(", ", config.TransportOptions?.Select(kv => $"{kv.Key}={kv.Value}") ?? []);
        _logger.CreatingTransport(endpointName, config.TransportType, options);

        if (string.Equals(config.TransportType, TransportTypes.StdIo, StringComparison.OrdinalIgnoreCase))
        {
            return new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = GetCommand(config),
                Arguments = config.TransportOptions?.GetValueOrDefault(ARGUMENTS_OPTIONS_KEY)?.Split(' '),
                WorkingDirectory = config.TransportOptions?.GetValueOrDefault("workingDirectory"),
                EnvironmentVariables = config.TransportOptions?
                    .Where(kv => kv.Key.StartsWith("env:", StringComparison.Ordinal))
                    .ToDictionary(kv => kv.Key.Substring(4), kv => kv.Value),
                ShutdownTimeout = TimeSpan.TryParse(config.TransportOptions?.GetValueOrDefault("shutdownTimeout"), CultureInfo.InvariantCulture, out var timespan) ? timespan : StdioClientTransportOptions.DefaultShutdownTimeout
            }, config, _loggerFactory);
        }

        if (string.Equals(config.TransportType, TransportTypes.Sse, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(config.TransportType, "http", StringComparison.OrdinalIgnoreCase))
        {
            return new SseClientTransport(
               new SseClientTransportOptions
               {
                   ConnectionTimeout = TimeSpan.FromSeconds(ParseOrDefault(config.TransportOptions, "connectionTimeout", 30)),
                   MaxReconnectAttempts = ParseOrDefault(config.TransportOptions, "maxReconnectAttempts", 3),
                   ReconnectDelay = TimeSpan.FromSeconds(ParseOrDefault(config.TransportOptions, "reconnectDelay", 5)),
                   AdditionalHeaders = config.TransportOptions?
                       .Where(kv => kv.Key.StartsWith("header.", StringComparison.Ordinal))
                       .ToDictionary(kv => kv.Key.Substring(7), kv => kv.Value)
               }, config, _loggerFactory);
        }

        throw new ArgumentException($"Unsupported transport type '{config.TransportType}'.", nameof(config));
    }

    private static int ParseOrDefault(Dictionary<string, string>? options, string key, int defaultValue)
    {
        if (options?.TryGetValue(key, out var value) ?? false)
        {
            if (!int.TryParse(value, out var result))
                throw new FormatException($"Invalid value '{value}' for option '{key}'");
            return result;
        }
        return defaultValue;
    }

    private static string GetCommand(McpServerConfig config)
    {
        var command = config.TransportOptions?.GetValueOrDefault(COMMAND_OPTIONS_KEY);

        if (string.IsNullOrEmpty(command))
            return config.Location!;

        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : command!;
    }

    /// <summary>
    /// Initializes a non-shell command by injecting a /c {command} argument, as the command will be run in a shell.
    /// </summary>
    private void InitializeCommand(McpServerConfig config)
    {
        string endpointName = $"Client ({config.Id}: {config.Name})";

        // If the command is empty or already contains cmd.exe, we don't need to do anything
        var command = config.TransportOptions?.GetValueOrDefault(COMMAND_OPTIONS_KEY);

        if (string.IsNullOrEmpty(command) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // On Windows, we need to wrap non-shell commands with cmd.exe /c
        if (command!.IndexOf("cmd.exe", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            _logger.SkippingShellWrapper(endpointName);
            return;
        }

        // If the command is not empty and does not contain cmd.exe, we need to inject /c {command} (usually npx or uvicorn)
        // This is because the stdio transport will not work correctly if the command is not run in a shell
        _logger.PromotingCommandToShellArgumentForStdio(endpointName, command, config.TransportOptions!.GetValueOrDefault(ARGUMENTS_OPTIONS_KEY) ?? "");
        config.TransportOptions![ARGUMENTS_OPTIONS_KEY] = config.TransportOptions.TryGetValue(ARGUMENTS_OPTIONS_KEY, out var args)
            ? $"/c {command} {args}"
            : $"/c {command}";
    }

    /// <summary>
    /// Disposes all clients created by the factory.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing && DisposeClientsOnDispose)
                DisposeClients();

            _isDisposed = true;
        }
    }

    private void DisposeClients()
    {
        foreach (var client in _clients.Values)
        {
            client?.DisposeAsync().AsTask().Wait();
        }

        _clients.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}