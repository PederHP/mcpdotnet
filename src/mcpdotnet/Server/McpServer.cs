
using System.Text.Json.Nodes;
using McpDotNet.Logging;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Shared;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Server;

/// <inheritdoc />
internal sealed class McpServer : McpJsonRpcEndpoint, IMcpServer
{
    private readonly IServerTransport _serverTransport;
    private readonly McpServerOptions _options;
    private volatile bool _isInitializing;
    private readonly ILogger<McpServer> _logger;

    private Func<RequestContext<ListToolsRequestParams>, CancellationToken, Task<ListToolsResult>>? _listToolsHandler;
    private Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>>? _callToolHandler;
    private Func<RequestContext<ListPromptsRequestParams>, CancellationToken, Task<ListPromptsResult>>? _listPromptsHandler;
    private Func<RequestContext<GetPromptRequestParams>, CancellationToken, Task<GetPromptResult>>? _getPromptHandler;
    private Func<RequestContext<ListResourcesRequestParams>, CancellationToken, Task<ListResourcesResult>>? _listResourcesHandler;
    private Func<RequestContext<ReadResourceRequestParams>, CancellationToken, Task<ReadResourceResult>>? _readResourceHandler;
    private Func<RequestContext<CompleteRequestParams>, CancellationToken, Task<CompleteResult>>? _getCompletionHandler;
    private Func<RequestContext<string>, CancellationToken, Task>? _subscribeToResourcesHandler;
    private Func<RequestContext<string>, CancellationToken, Task>? _unsubscribeFromResourcesHandler;

    /// <summary>
    /// Creates a new instance of <see cref="McpServer"/>.
    /// </summary>
    /// <param name="transport">Transport to use for the server</param>
    /// <param name="options">Configuration options for this server, including capabilities. 
    /// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>    
    /// <param name="loggerFactory">Logger factory to use for logging</param>
    /// <param name="serviceProvider">Optional service provider to use for dependency injection</param>
    /// <exception cref="McpServerException"></exception>
    public McpServer(IServerTransport transport, McpServerOptions options, ILoggerFactory loggerFactory, IServiceProvider? serviceProvider)
        : base(transport, loggerFactory)
    {
        _serverTransport = transport;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory.CreateLogger<McpServer>();
        ServerInstructions = options.ServerInstructions;
        ServiceProvider = serviceProvider;

        SetToolsHandler(options);
        SetPromptsHandler(options);
        SetResourcesHandler(options);
        SetCompletionHandler();
        SetInitializeHandler(options);
        SetPingHandler();
        AddNotificationHandler();
    }

    public ClientCapabilities? ClientCapabilities { get; set; }

    /// <inheritdoc />
    public Implementation? ClientInfo { get; set; }

    /// <inheritdoc />
    public string? ServerInstructions { get; set; }

    /// <inheritdoc />
    public IServiceProvider? ServiceProvider { get; }

    /// <inheritdoc />
    public override string EndpointName =>
        $"Server ({_options.ServerInfo.Name} {_options.ServerInfo.Version}), Client ({ClientInfo?.Name} {ClientInfo?.Version})";

    public void SetOperationHandler(string operationName, Delegate handler)
    {
        if (operationName is null)
        {
            throw new ArgumentNullException(nameof(operationName));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (!TrySetOperationHandler(OperationNames.ListTools, operationName, handler, ref _listToolsHandler) &&
            !TrySetOperationHandler(OperationNames.CallTool, operationName, handler, ref _callToolHandler) &&
            !TrySetOperationHandler(OperationNames.ListPrompts, operationName, handler, ref _listPromptsHandler) &&
            !TrySetOperationHandler(OperationNames.GetPrompt, operationName, handler, ref _getPromptHandler) &&
            !TrySetOperationHandler(OperationNames.ListResources, operationName, handler, ref _listResourcesHandler) &&
            !TrySetOperationHandler(OperationNames.ReadResource, operationName, handler, ref _readResourceHandler) &&
            !TrySetOperationHandler(OperationNames.GetCompletion, operationName, handler, ref _getCompletionHandler) &&
            !TrySetOperationHandler(OperationNames.SubscribeToResources, operationName, handler, ref _subscribeToResourcesHandler) &&
            !TrySetOperationHandler(OperationNames.UnsubscribeFromResources, operationName, handler, ref _unsubscribeFromResourcesHandler))
        {
            throw new ArgumentException($"Unknown operation '{operationName}'", nameof(operationName));
        }

        static bool TrySetOperationHandler<TFieldRequest, TFieldResponse>(
            string targetOperationName,
            string operationName,
            Delegate handler,
            ref Func<TFieldRequest, CancellationToken, TFieldResponse>? field)
        {
            if (operationName == targetOperationName)
            {
                if (handler is Func<TFieldRequest, CancellationToken, TFieldResponse> typed)
                {
                    field = typed;
                    return true;
                }

                throw new ArgumentException(
                    $"Handler must be of type {typeof(Func<TFieldRequest, CancellationToken, TFieldResponse>)}",
                    nameof(handler));
            }

            return false;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitializing)
        {
            _logger.ServerAlreadyInitializing(EndpointName);
            throw new InvalidOperationException("Server is already initializing");
        }
        _isInitializing = true;

        if (IsInitialized)
        {
            _logger.ServerAlreadyInitializing(EndpointName);
            return;
        }

        try
        {
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start listening for messages
            await _serverTransport.StartListeningAsync(CancellationTokenSource.Token).ConfigureAwait(false);

            // Start processing messages
            MessageProcessingTask = ProcessMessagesAsync(CancellationTokenSource.Token);

            // Unlike McpClient, we're not done initializing until we've received a message from the client, so we don't set IsInitialized here
        }
        catch (Exception e)
        {
            _logger.ServerInitializationError(EndpointName, e);
            await CleanupAsync().ConfigureAwait(false);
            throw;
        }
    }

    private void AddNotificationHandler()
    {
        AddNotificationHandler("notifications/initialized", (notification) =>
        {
            IsInitialized = true;
            return Task.CompletedTask;
        });
    }

    private void SetPingHandler()
    {
        SetRequestHandler<JsonNode, PingResult>("ping", 
            request => Task.FromResult(new PingResult()));
    }

    private void SetInitializeHandler(McpServerOptions options)
    {
        SetRequestHandler<InitializeRequestParams, InitializeResult>("initialize",
                    (request) =>
                    {
                        ClientCapabilities = request?.Capabilities ?? new();
                        ClientInfo = request?.ClientInfo;
                        return Task.FromResult(new InitializeResult()
                        {
                            ProtocolVersion = options.ProtocolVersion,
                            Instructions = ServerInstructions,
                            ServerInfo = _options.ServerInfo,
                            Capabilities = options.Capabilities ?? new ServerCapabilities(),
                        });
                    });
    }

    private void SetCompletionHandler()
    {
        SetRequestHandler<CompleteRequestParams, CompleteResult>("completion/complete",
                        async (request) =>
                        {
                            if (_getCompletionHandler is null)
                            {
                                // This capability is not optional, so return an empty result if there is no handler
                                return new CompleteResult()
                                {
                                    Completion = new()
                                    {
                                        Values = [],
                                        Total = 0,
                                        HasMore = false
                                    }
                                };
                            }

                            return await _getCompletionHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                        });
    }

    private void SetResourcesHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Resources is not null)
        {
            SetRequestHandler<ListResourcesRequestParams, ListResourcesResult>("resources/list",
                async (request) =>
                {
                    if (_listResourcesHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListResourcesHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListResources handler not configured");
                    }

                    return await _listResourcesHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<ReadResourceRequestParams, ReadResourceResult>("resources/read",
                async (request) =>
                {
                    if (_readResourceHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ReadResourceHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ReadResource handler not configured");
                    }

                    return await _readResourceHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }

    private void SetPromptsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Prompts is not null)
        {
            SetRequestHandler<ListPromptsRequestParams, ListPromptsResult>("prompts/list",
                async (request) =>
                {
                    if (_listPromptsHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListPromptsHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListPrompts handler not configured");
                    }

                    return await _listPromptsHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<GetPromptRequestParams, GetPromptResult>("prompts/get",
                async (request) =>
                {
                    if (_getPromptHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.GetPromptHandlerNotConfigured(EndpointName);
                        throw new McpServerException("GetPrompt handler not configured");
                    }

                    return await _getPromptHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }

    private void SetToolsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Tools is not null)
        {
            SetRequestHandler<ListToolsRequestParams, ListToolsResult>("tools/list",
                async (request) =>
                {
                    if (_listToolsHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListToolsHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListTools handler not configured");
                    }

                    return await _listToolsHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<CallToolRequestParams, CallToolResponse>("tools/call",
                async (request) =>
                {
                    if (_callToolHandler is null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.CallToolHandlerNotConfigured(EndpointName);
                        throw new McpServerException("CallTool handler not configured");
                    }

                    return await _callToolHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }
}
