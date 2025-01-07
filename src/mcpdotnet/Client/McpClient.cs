namespace McpDotNet.Client;

using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Transport;
using System.Collections.Concurrent;
using McpDotNet.Protocol.Messages;
using System.Diagnostics;
using System.Text.Json;
using McpDotNet.Utils.Json;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;
using McpDotNet.Configuration;

/// <inheritdoc/>
internal class McpClient : IMcpClient
{
    private readonly IMcpTransport _transport;
    private readonly McpClientOptions _options;
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<IJsonRpcMessage>> _pendingRequests;
    private readonly ConcurrentDictionary<string, List<Func<JsonRpcNotification,Task>>> _notificationHandlers;
    private readonly Dictionary<string, Func<JsonRpcRequest, Task<object>>> _requestHandlers = new();
    private int _nextRequestId;
    private readonly McpServerConfig _serverConfig;
    private bool _isInitialized;
    private Task? _messageProcessingTask;
    private CancellationTokenSource? _cts;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions().ConfigureForMcp();
    private readonly ILogger<McpClient> _logger;
    private volatile bool _isInitializing;

    /// <inheritdoc/>
    public bool IsInitialized => _isInitialized;    

    /// <inheritdoc/>
    public ServerCapabilities? ServerCapabilities { get; private set; }

    /// <inheritdoc/>
    public Implementation? ServerInfo { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClient"/> class.
    /// </summary>
    /// <param name="transport">An MCP transport implementation.</param>
    /// <param name="options">Options for the client, defining protocol version and capabilities.</param>
    /// <param name="serverConfig">The server configuration.</param>
    /// <param name="logger">A logger for the client.</param>
    public McpClient(IMcpTransport transport, McpClientOptions options, McpServerConfig serverConfig, ILogger<McpClient> logger)
    {
        _transport = transport;
        _options = options;
        _pendingRequests = new();
        _notificationHandlers = new();
        _nextRequestId = 1;
        _serverConfig = serverConfig;
        _logger = logger;

        if (options.Capabilities?.Sampling != null)
        {
            SetRequestHandler<CreateMessageRequestParams, CreateMessageResult>("sampling/createMessage",
                async (request) => {
                    if (SamplingHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.SamplingHandlerNotConfigured(_serverConfig);
                        throw new McpClientException("Sampling handler not configured");
                    }

                    return await SamplingHandler(request);
                });
        }
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitializing)
        {
            _logger.ClientAlreadyInitializing(_serverConfig);
            throw new InvalidOperationException("Client is already initializing");
        }
        _isInitializing = true;

        if (_isInitialized)
        {
            _logger.ClientAlreadyInitialized(_serverConfig);
            return;
        }

        try
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Connect transport
            await _transport.ConnectAsync(_cts.Token).ConfigureAwait(false);

            // Start processing messages
            _messageProcessingTask = ProcessMessagesAsync(_cts.Token);

            // Perform initialization sequence
            await InitializeAsync(_cts.Token).ConfigureAwait(false);

            _isInitialized = true;
        }
        catch (Exception e)
        {
            _logger.ClientInitializationError(_serverConfig, e);
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
            _logger.ServerCapabilitiesReceived(_serverConfig, initializeResponse.Capabilities, initializeResponse.ServerInfo);
            ServerCapabilities = initializeResponse.Capabilities;
            ServerInfo = initializeResponse.ServerInfo;

            // Validate protocol version
            if (initializeResponse.ProtocolVersion != _options.ProtocolVersion)
            {
                _logger.ServerProtocolVersionMismatch(_serverConfig, _options.ProtocolVersion, initializeResponse.ProtocolVersion);
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
            _logger.ClientInitializationTimeout(_serverConfig);
            throw new McpClientException("Initialization timed out");
        }
    }

    /// <inheritdoc/>
    public async Task PingAsync(CancellationToken cancellationToken)
    {
        _logger.PingingServer(_serverConfig);
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
        _logger.ListingTools(_serverConfig,cursor);
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
        _logger.ListingPrompts(_serverConfig, cursor);
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
        _logger.GettingPrompt(_serverConfig, name, arguments);
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
        _logger.ListingResources(_serverConfig, cursor);
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
        _logger.ReadingResource(_serverConfig, uri);
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
    public async Task SubscribeToResourceAsync(string uri, CancellationToken cancellationToken = default)
    {
        _logger.SubscribingToResource(_serverConfig, uri);
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
        _logger.UnsubscribingFromResource(_serverConfig, uri);
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
    public async Task<CallToolResponse> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        _logger.CallingTool(_serverConfig, toolName, arguments);
        return await SendRequestAsync<CallToolResponse>(
            new JsonRpcRequest
            {
                Method = "tools/call",
                Params = CreateParametersDictionary(toolName, arguments)
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private static Dictionary<string, object?> CreateParametersDictionary(string nameParameter, Dictionary<string, object>? optionalArguments = null)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["name"] = nameParameter
        };

        if (optionalArguments != null)
        {
            parameters["arguments"] = optionalArguments;
        }

        return parameters;
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _transport.MessageReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await HandleMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
            _logger.ClientMessageProcessingCancelled(_serverConfig);
        }
        catch (NullReferenceException)
        {
            // Ignore reader disposal and mocked transport
        }
    }

    private async Task HandleMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case JsonRpcRequest request:
                if (_requestHandlers.TryGetValue(request.Method, out var handler))
                {
                    try
                    {
                        _logger.RequestHandlerCalled(_serverConfig, request.Method);    
                        var result = await handler(request);
                        _logger.RequestHandlerCompleted(_serverConfig, request.Method);
                        await _transport.SendMessageAsync(new JsonRpcResponse
                        {
                            Id = request.Id,
                            JsonRpc = "2.0",
                            Result = result
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.RequestHandlerError(_serverConfig, request.Method, ex);
                        // Send error response
                        await _transport.SendMessageAsync(new JsonRpcError
                        {
                            Id = request.Id,
                            JsonRpc = "2.0",
                            Error = new JsonRpcErrorDetail
                            {
                                Code = -32000,  // Implementation defined error
                                Message = ex.Message
                            }
                        }, cancellationToken);
                    }
                }
                break;
            case IJsonRpcMessageWithId messageWithId:
                if (_pendingRequests.TryRemove(messageWithId.Id, out var tcs))
                {
                    tcs.TrySetResult(message);
                }
                else
                {
                    _logger.NoRequestFoundForMessageWithId(_serverConfig, messageWithId);
                }
                break;

            case JsonRpcNotification notification:
                if (_notificationHandlers.TryGetValue(notification.Method, out var handlers))
                {
                    foreach (var notificationHandler in handlers)
                    {
                        try
                        {
                            await notificationHandler(notification).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            // Log handler error but continue processing
                            Debug.WriteLine($"Notification handler error: {ex}");
                        }
                    }
                }
                break;
        }
    }

    /// <inheritdoc/>
    public async Task<T> SendRequestAsync<T>(JsonRpcRequest request, CancellationToken cancellationToken) where T : class
    {
        if (!_transport.IsConnected)
        {
            throw new McpClientException("Transport is not connected");
        }

        // Set request ID
        request.Id = RequestId.FromNumber(Interlocked.Increment(ref _nextRequestId));

        var tcs = new TaskCompletionSource<IJsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.Id] = tcs;

        try
        {
            Debug.WriteLine($"Sending initialize request: {JsonSerializer.Serialize(request)}");

            await _transport.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
            var response = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (response is JsonRpcError error)
            {
                throw new McpClientException($"Request failed: {error.Error.Message}", error.Error.Code);
            }

            if (response is JsonRpcResponse success)
            {
                // Convert the Result object to JSON and back to get our strongly-typed result
                var resultJson = JsonSerializer.Serialize(success.Result, _jsonOptions);
                var resultObject = JsonSerializer.Deserialize<T>(resultJson, _jsonOptions);
                if (resultObject != null)
                {
                    return resultObject;
                }
                Debug.WriteLine($"Received response: {JsonSerializer.Serialize(success.Result)}");
                Debug.WriteLine($"Expected type: {typeof(T)}");
                throw new McpClientException($"Unexpected response type {JsonSerializer.Serialize(success.Result)}, expected {typeof(T)}");
            }

            throw new McpClientException("Invalid response type");
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    private async Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
        {
            throw new McpClientException("Transport is not connected");
        }

        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void OnNotification(string method, Func<JsonRpcNotification,Task> handler)
    {
        var handlers = _notificationHandlers.GetOrAdd(method, _ => new());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public Func<CreateMessageRequestParams, Task<CreateMessageResult>>? SamplingHandler { get; set; }

    private void SetRequestHandler<TRequest, TResponse>(string method, Func<TRequest, Task<TResponse>> handler)
    {
        _requestHandlers[method] = async (request) => {
            // Convert the params JsonElement to our type using the same options
            var jsonString = JsonSerializer.Serialize(request.Params);
            var typedRequest = JsonSerializer.Deserialize<TRequest>(jsonString, _jsonOptions);
            return await handler(typedRequest);
        };
    }

    private async Task CleanupAsync()
    {
        _cts?.Cancel();

        if (_messageProcessingTask != null)
        {
            try
            {
                await _messageProcessingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
        }

        // Complete all pending requests with cancellation
        foreach (var (_, tcs) in _pendingRequests)
        {
            tcs.TrySetCanceled();
        }
        _pendingRequests.Clear();

        await _transport.DisposeAsync().ConfigureAwait(false);
        _cts?.Dispose();

        _isInitialized = false;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await CleanupAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

