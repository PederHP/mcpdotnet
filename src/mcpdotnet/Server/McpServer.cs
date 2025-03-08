
using System.Text.Json.Nodes;
using McpDotNet.Logging;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Shared;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Server;

/// <inheritdoc />
internal class McpServer : McpJsonRpcEndpoint, IMcpServer
{
    private readonly IServerTransport _serverTransport;
    private readonly McpServerOptions _options;
    private volatile bool _isInitializing;
    private readonly ILogger<McpServer> _logger;

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
        _options = options;
        _logger = loggerFactory.CreateLogger<McpServer>();
        ServerInstructions = options.ServerInstructions;
        ServiceProvider = serviceProvider;

        SetToolsHandler(options);
        SetPromptsHandler(options);
        SetResourcesHandler(options);
        SetCompletionHandler();
        SetInitializeHandler(options);
        SetPingHandler();
        SetNotificationHandler();
    }

    public ClientCapabilities? ClientCapabilities { get; set; }

    /// <inheritdoc />
    public Implementation? ClientInfo { get; set; }

    /// <inheritdoc />
    public string? ServerInstructions { get; set; }

    /// <inheritdoc />
    public IServiceProvider? ServiceProvider { get; }

    /// <inheritdoc />
    public Func<RequestContext<ListToolsRequestParams>, CancellationToken, Task<ListToolsResult>>? ListToolsHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>>? CallToolHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<ListPromptsRequestParams>, CancellationToken, Task<ListPromptsResult>>? ListPromptsHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<GetPromptRequestParams>, CancellationToken, Task<GetPromptResult>>? GetPromptHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<ListResourcesRequestParams>, CancellationToken, Task<ListResourcesResult>>? ListResourcesHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<ReadResourceRequestParams>, CancellationToken, Task<ReadResourceResult>>? ReadResourceHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<CompleteRequestParams>, CancellationToken, Task<CompleteResult>>? GetCompletionHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<string>, CancellationToken, Task>? SubscribeToResourcesHandler { get; set; }

    /// <inheritdoc />
    public Func<RequestContext<string>, CancellationToken, Task>? UnsubscribeFromResourcesHandler { get; set; }

    /// <inheritdoc />
    public override string EndpointName
    {
        get
        {
            return $"Server ({_options.ServerInfo.Name} {_options.ServerInfo.Version}), Client ({ClientInfo?.Name} {ClientInfo?.Version})";
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

    /// <inheritdoc />
    public async Task<CreateMessageResult> RequestSamplingAsync(CreateMessageRequestParams request, CancellationToken cancellationToken)
    {
        if (ClientCapabilities?.Sampling == null)
        {
            throw new McpServerException("Client does not support sampling");
        }

        return await SendRequestAsync<CreateMessageResult>(
            new JsonRpcRequest
            {
                Method = "sampling/createMessage",
                Params = request
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ListRootsResult> RequestRootsAsync(ListRootsRequestParams request, CancellationToken cancellationToken)
    {
        if (ClientCapabilities?.Roots == null)
        {
            throw new McpServerException("Client does not support roots");
        }

        return await SendRequestAsync<ListRootsResult>(
            new JsonRpcRequest
            {
                Method = "roots/list",
                Params = request
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private void SetNotificationHandler()
    {
        OnNotification("notifications/initialized", (notification) =>
        {
            IsInitialized = true;
            return Task.CompletedTask;
        });
    }

    private void SetPingHandler()
    {
        SetRequestHandler<JsonNode, PingResult>("ping",
                    (request) =>
                    {
                        return Task.FromResult(new PingResult());
                    });
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
                            if (GetCompletionHandler == null)
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

                            return await GetCompletionHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                        });
    }

    private void SetResourcesHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Resources != null)
        {
            SetRequestHandler<ListResourcesRequestParams, ListResourcesResult>("resources/list",
                async (request) =>
                {
                    if (ListResourcesHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListResourcesHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListResources handler not configured");
                    }

                    return await ListResourcesHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<ReadResourceRequestParams, ReadResourceResult>("resources/read",
                async (request) =>
                {
                    if (ReadResourceHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ReadResourceHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ReadResource handler not configured");
                    }

                    return await ReadResourceHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }

    private void SetPromptsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Prompts != null)
        {
            SetRequestHandler<ListPromptsRequestParams, ListPromptsResult>("prompts/list",
                async (request) =>
                {
                    if (ListPromptsHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListPromptsHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListPrompts handler not configured");
                    }

                    return await ListPromptsHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<GetPromptRequestParams, GetPromptResult>("prompts/get",
                async (request) =>
                {
                    if (GetPromptHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.GetPromptHandlerNotConfigured(EndpointName);
                        throw new McpServerException("GetPrompt handler not configured");
                    }

                    return await GetPromptHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }

    private void SetToolsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Tools != null)
        {
            SetRequestHandler<ListToolsRequestParams, ListToolsResult>("tools/list",
                async (request) =>
                {
                    if (ListToolsHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.ListToolsHandlerNotConfigured(EndpointName);
                        throw new McpServerException("ListTools handler not configured");
                    }

                    return await ListToolsHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });

            SetRequestHandler<CallToolRequestParams, CallToolResponse>("tools/call",
                async (request) =>
                {
                    if (CallToolHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.CallToolHandlerNotConfigured(EndpointName);
                        throw new McpServerException("CallTool handler not configured");
                    }

                    return await CallToolHandler(new(this, request), CancellationTokenSource?.Token ?? CancellationToken.None).ConfigureAwait(false);
                });
        }
    }
}
