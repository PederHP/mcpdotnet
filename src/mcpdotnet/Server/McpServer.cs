using System.Text.Json.Nodes;
using McpDotNet.Logging;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Shared;
using McpDotNet.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Server;

/// <inheritdoc />
internal sealed class McpServer : McpJsonRpcEndpoint, IMcpServer
{
    private readonly IServerTransport? _serverTransport;
    private readonly McpServerOptions _options;
    private volatile bool _isInitializing;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="McpServer"/>.
    /// </summary>
    /// <param name="transport">Transport to use for the server</param>
    /// <param name="options">Configuration options for this server, including capabilities. 
    /// Make sure to accurately reflect exactly what capabilities the server supports and does not support.</param>    
    /// <param name="loggerFactory">Logger factory to use for logging</param>
    /// <param name="serviceProvider">Optional service provider to use for dependency injection</param>
    /// <exception cref="McpServerException"></exception>
    public McpServer(ITransport transport, McpServerOptions options, ILoggerFactory? loggerFactory, IServiceProvider? serviceProvider)
        : base(transport, loggerFactory)
    {
        Throw.IfNull(options);

        _serverTransport = transport as IServerTransport;
        _options = options;
        _logger = (ILogger?)loggerFactory?.CreateLogger<McpServer>() ?? NullLogger.Instance;
        ServerInstructions = options.ServerInstructions;
        ServiceProvider = serviceProvider;

        AddNotificationHandler("notifications/initialized", _ =>
        {
            IsInitialized = true;
            return Task.CompletedTask;
        });

        SetInitializeHandler(options);
        SetCompletionHandler(options);
        SetPingHandler();
        SetToolsHandler(options);
        SetPromptsHandler(options);
        SetResourcesHandler(options);
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

            if (_serverTransport is not null)
            {
                // Start listening for messages
                await _serverTransport.StartListeningAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            }

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

    private void SetPingHandler()
    {
        SetRequestHandler<JsonNode, PingResult>("ping", 
            request => Task.FromResult(new PingResult()));
    }

    private void SetInitializeHandler(McpServerOptions options)
    {
        SetRequestHandler<InitializeRequestParams, InitializeResult>("initialize",
            request =>
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

    private void SetCompletionHandler(McpServerOptions options)
    {
        // This capability is not optional, so return an empty result if there is no handler.
        SetRequestHandler<CompleteRequestParams, CompleteResult>("completion/complete",
            options.GetCompletionHandler is { } handler ?
                request => handler(new(this, request), CancellationTokenSource?.Token ?? default) :
                request => Task.FromResult(new CompleteResult() { Completion = new() { Values = [], Total = 0, HasMore = false } }));
    }

    private void SetResourcesHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Resources is not { } resourcesCapability)
        {
            return;
        }

        if (resourcesCapability.ListResourcesHandler is not { } listResourcesHandler ||
            resourcesCapability.ReadResourceHandler is not { } readResourceHandler)
        {
            throw new McpServerException("Resources capability was enabled, but ListResources and/or ReadResource handlers were not specified.");
        }

        CancellationToken cancellationToken = CancellationTokenSource?.Token ?? default;
        SetRequestHandler<ListResourcesRequestParams, ListResourcesResult>("resources/list", request => listResourcesHandler(new(this, request), cancellationToken));
        SetRequestHandler<ReadResourceRequestParams, ReadResourceResult>("resources/read", request => readResourceHandler(new(this, request), cancellationToken));

        if (resourcesCapability.Subscribe is not true)
        {
            return;
        }

        var subscribeHandler = resourcesCapability.SubscribeToResourcesHandler;
        var unsubscribeHandler = resourcesCapability.UnsubscribeFromResourcesHandler;
        if (subscribeHandler is null || unsubscribeHandler is null)
        {
            throw new McpServerException("Resources capability was enabled with subscribe support, but SubscribeToResources and/or UnsubscribeFromResources handlers were not specified.");
        }

        SetRequestHandler<SubscribeRequestParams, EmptyResult>("resources/subscribe", request => subscribeHandler(new(this, request), cancellationToken));
        SetRequestHandler<UnsubscribeRequestParams, EmptyResult>("resources/unsubscribe", request => unsubscribeHandler(new(this, request), cancellationToken));
    }

    private void SetPromptsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Prompts is not { } promptsCapability)
        {
            return;
        }

        if (promptsCapability.ListPromptsHandler is not { } listPromptsHandler ||
            promptsCapability.GetPromptHandler is not { } getPromptHandler)
        {
            throw new McpServerException("Prompts capability was enabled, but ListPrompts and/or GetPrompt handlers were not specified.");
        }

        CancellationToken cancellationToken = CancellationTokenSource?.Token ?? default;
        SetRequestHandler<ListPromptsRequestParams, ListPromptsResult>("prompts/list", request => listPromptsHandler(new(this, request), cancellationToken));
        SetRequestHandler<GetPromptRequestParams, GetPromptResult>("prompts/get", request => getPromptHandler(new(this, request), cancellationToken));
    }

    private void SetToolsHandler(McpServerOptions options)
    {
        if (options.Capabilities?.Tools is not { } toolsCapability)
        {
            return;
        }

        if (toolsCapability.ListToolsHandler is not { } listToolsHandler ||
            toolsCapability.CallToolHandler is not { } callToolHandler)
        {
            throw new McpServerException("ListTools and/or CallTool handlers were specified but the Tools capability was not enabled.");
        }

        CancellationToken cancellationToken = CancellationTokenSource?.Token ?? default;
        SetRequestHandler<ListToolsRequestParams, ListToolsResult>("tools/list", request => listToolsHandler(new(this, request), cancellationToken));
        SetRequestHandler<CallToolRequestParams, CallToolResponse>("tools/call", request => callToolHandler(new(this, request), cancellationToken));
    }
}
