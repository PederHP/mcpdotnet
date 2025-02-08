
using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Messages;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;
using McpDotNet.Configuration;
using McpDotNet.Shared;

namespace McpDotNet.Client;

/// <inheritdoc/>
internal class McpClient : McpJsonRpcEndpoint, IMcpClient
{
    private readonly McpClientOptions _options;
    private readonly McpServerConfig _serverConfig;
    private readonly ILogger<McpClient> _logger;
    private volatile bool _isInitializing;
    private readonly IClientTransport _clientTransport;

    /// <inheritdoc/>
    public ServerCapabilities? ServerCapabilities { get; private set; }

    /// <inheritdoc/>
    public Implementation? ServerInfo { get; private set; }

    /// <inheritdoc/>
    public string? ServerInstructions { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClient"/> class.
    /// </summary>
    /// <param name="transport">The transport to use for communication with the server.</param>
    /// <param name="options">Options for the client, defining protocol version and capabilities.</param>
    /// <param name="serverConfig">The server configuration.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public McpClient(IClientTransport transport, McpClientOptions options, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
        : base(transport, loggerFactory)
    {
        _options = options;
        _serverConfig = serverConfig;
        _logger = loggerFactory.CreateLogger<McpClient>();
        _clientTransport = transport;

        if (options.Capabilities?.Sampling != null)
        {
            SetRequestHandler<CreateMessageRequestParams, CreateMessageResult>("sampling/createMessage",
                async (request) => {
                    if (SamplingHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.SamplingHandlerNotConfigured(EndpointName);
                        throw new McpClientException("Sampling handler not configured");
                    }

                    return await SamplingHandler(request, CancellationTokenSource?.Token ?? CancellationToken.None);
                });
        }
        if (options.Capabilities?.Roots != null)
        {
            SetRequestHandler<ListRootsRequestParams, ListRootsResult>("roots/list",
                async (request) =>
                {
                    if (RootsHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.RootsHandlerNotConfigured(EndpointName);
                        throw new McpClientException("Roots handler not configured");
                    }
                    return await RootsHandler(request, CancellationTokenSource?.Token ?? CancellationToken.None);
                });
        }
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitializing)
        {
            _logger.ClientAlreadyInitializing(EndpointName);
            throw new InvalidOperationException("Client is already initializing");
        }
        _isInitializing = true;

        if (IsInitialized)
        {
            _logger.ClientAlreadyInitialized(EndpointName);
            return;
        }

        try
        {
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Connect transport
            await _clientTransport.ConnectAsync(CancellationTokenSource.Token).ConfigureAwait(false);

            // Start processing messages
            MessageProcessingTask = ProcessMessagesAsync(CancellationTokenSource.Token);

            // Perform initialization sequence
            await InitializeAsync(CancellationTokenSource.Token).ConfigureAwait(false);

            IsInitialized = true;
        }
        catch (Exception e)
        {
            _logger.ClientInitializationError(EndpointName, e);
            await CleanupAsync().ConfigureAwait(false);
            throw;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var initializationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        initializationCts.CancelAfter(_options.InitializationTimeout);

        try
        {
            // Send initialize request
            var initializeResponse = await SendRequestAsync<InitializeResult>(
                new JsonRpcRequest
                {
                    Method = "initialize",
                    Params = new
                    {
                        protocolVersion = _options.ProtocolVersion,
                        capabilities = _options.Capabilities ?? new ClientCapabilities(),
                        clientInfo = _options.ClientInfo
                    }
                },
                initializationCts.Token
            ).ConfigureAwait(false);

            // Store server information
            _logger.ServerCapabilitiesReceived(EndpointName, JsonSerializer.Serialize(initializeResponse.Capabilities), JsonSerializer.Serialize(initializeResponse.ServerInfo));
            ServerCapabilities = initializeResponse.Capabilities;
            ServerInfo = initializeResponse.ServerInfo;
            ServerInstructions = initializeResponse.Instructions;

            // Validate protocol version
            if (initializeResponse.ProtocolVersion != _options.ProtocolVersion)
            {
                _logger.ServerProtocolVersionMismatch(EndpointName, _options.ProtocolVersion, initializeResponse.ProtocolVersion);
                throw new McpClientException($"Server protocol version mismatch. Expected {_options.ProtocolVersion}, got {initializeResponse.ProtocolVersion}");
            }

            // Send initialized notification
            await SendNotificationAsync(
                new JsonRpcNotification
                {
                    Method = "notifications/initialized"
                },
                initializationCts.Token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (initializationCts.IsCancellationRequested)
        {
            _logger.ClientInitializationTimeout(EndpointName);
            throw new McpClientException("Initialization timed out");
        }
    }

    /// <inheritdoc/>
    public async Task PingAsync(CancellationToken cancellationToken)
    {
        _logger.PingingServer(EndpointName);
        await SendRequestAsync<dynamic>(
            new JsonRpcRequest
            {
                Method = "ping"
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ListToolsResult> ListToolsAsync(string? cursor = null, CancellationToken cancellationToken = default)
    {
        _logger.ListingTools(EndpointName, cursor ?? "(null)");
        return await SendRequestAsync<ListToolsResult>(
            new JsonRpcRequest
            {
                Method = "tools/list",
                Params = cursor != null ? new Dictionary<string, object?> { ["cursor"] = cursor } : null
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ListPromptsResult> ListPromptsAsync(string? cursor = null, CancellationToken cancellationToken = default)
    {
        _logger.ListingPrompts(EndpointName, cursor ?? "(null)");
        return await SendRequestAsync<ListPromptsResult>(
            new JsonRpcRequest
            {
                Method = "prompts/list",
                Params = cursor != null ? new Dictionary<string, object?> { ["cursor"] = cursor } : null
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<GetPromptResult> GetPromptAsync(string name, Dictionary<string, object>? arguments = null, CancellationToken cancellationToken = default)
    {
        _logger.GettingPrompt(EndpointName, name, arguments == null ? "{}" : JsonSerializer.Serialize(arguments));
        return await SendRequestAsync<GetPromptResult>(
            new JsonRpcRequest
            {
                Method = "prompts/get",
                Params = CreateParametersDictionary(name, arguments)
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ListResourcesResult> ListResourcesAsync(string? cursor = null, CancellationToken cancellationToken = default)
    {
        _logger.ListingResources(EndpointName, cursor ?? "(null)");
        return await SendRequestAsync<ListResourcesResult>(
            new JsonRpcRequest
            {
                Method = "resources/list",
                Params = cursor != null ? new Dictionary<string, object?> { ["cursor"] = cursor } : null
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ReadResourceResult> ReadResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.ReadingResource(EndpointName, uri);
        return await SendRequestAsync<ReadResourceResult>(
            new JsonRpcRequest
            {
                Method = "resources/read",
                Params = new Dictionary<string, object?>
                {
                    ["uri"] = uri
                }
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<CompleteResult> GetCompletionAsync(Reference reference, string argumentName, string argumentValue, CancellationToken cancellationToken = default)
    {
        if (!reference.Validate(out string validationMessage))
        {
            _logger.InvalidCompletionReference(EndpointName, reference.ToString(), validationMessage);
            throw new McpClientException($"Invalid reference: {validationMessage}");
        }
        if (string.IsNullOrWhiteSpace(argumentName))
        {
            _logger.InvalidCompletionArgumentName(EndpointName, argumentName);
            throw new McpClientException("Argument name cannot be null or empty");
        }
        if (argumentValue is null)
        {
            _logger.InvalidCompletionArgumentValue(EndpointName, argumentValue);
            throw new McpClientException("Argument value cannot be null");
        }

        _logger.GettingCompletion(EndpointName, reference.ToString(), argumentName, argumentValue);
        return await SendRequestAsync<CompleteResult>(
            new JsonRpcRequest
            {
                Method = "completion/complete",
                Params = new Dictionary<string, object?>
                {
                    ["ref"] = reference,
                    ["argument"] = new Argument { Name = argumentName, Value = argumentValue }
                }
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SubscribeToResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.SubscribingToResource(EndpointName, uri);
        await SendRequestAsync<dynamic>(
            new JsonRpcRequest
            {
                Method = "resources/subscribe",
                Params = new Dictionary<string, object?>
                {
                    ["uri"] = uri
                }
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task UnsubscribeFromResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.UnsubscribingFromResource(EndpointName, uri);
        await SendRequestAsync<dynamic>(
            new JsonRpcRequest
            {
                Method = "resources/unsubscribe",
                Params = new Dictionary<string, object?>
                {
                    ["uri"] = uri
                }
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<CallToolResponse> CallToolAsync(string toolName, Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        _logger.CallingTool(EndpointName, toolName, JsonSerializer.Serialize(arguments));
        return await SendRequestAsync<CallToolResponse>(
            new JsonRpcRequest
            {
                Method = "tools/call",
                Params = CreateParametersDictionary(toolName, arguments ?? new())
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static Dictionary<string, object?> CreateParametersDictionary(string nameParameter, Dictionary<string, object> arguments)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["name"] = nameParameter
        };

        if (arguments != null)
        {
            parameters["arguments"] = arguments;
        }

        return parameters;
    }

    /// <inheritdoc/>
    public Func<CreateMessageRequestParams, CancellationToken, Task<CreateMessageResult>>? SamplingHandler { get; set; }

    /// <inheritdoc/>
    public Func<ListRootsRequestParams, CancellationToken, Task<ListRootsResult>>? RootsHandler { get; set; }

    /// <inheritdoc/>
    public override string EndpointName
    {
        get
        {
            return $"Client ({_serverConfig.Id}: {_serverConfig.Name})";
        }
    }
}
