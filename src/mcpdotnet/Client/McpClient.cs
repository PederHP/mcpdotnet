
using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Transport;
using System.Collections.Concurrent;
using McpDotNet.Protocol.Messages;
using System.Text.Json;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;
using McpDotNet.Configuration;

namespace McpDotNet.Client;

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
    private readonly JsonSerializerOptions _jsonOptions;
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
    /// <param name="loggerFactory">The logger factory.</param>
    public McpClient(IMcpTransport transport, McpClientOptions options, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
    {
        _transport = transport;
        _options = options;
        _pendingRequests = new();
        _notificationHandlers = new();
        _nextRequestId = 1;
        _serverConfig = serverConfig;
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
        _logger = loggerFactory.CreateLogger<McpClient>();

        if (options.Capabilities?.Sampling != null)
        {
            SetRequestHandler<CreateMessageRequestParams, CreateMessageResult>("sampling/createMessage",
                async (request) => {
                    if (SamplingHandler == null)
                    {
                        // Setting the capability, but not a handler means we have nothing to return to the server
                        _logger.SamplingHandlerNotConfigured(_serverConfig.Id, _serverConfig.Name);
                        throw new McpClientException("Sampling handler not configured");
                    }

                    return await SamplingHandler(request, _cts?.Token ?? CancellationToken.None);
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
                        _logger.RootsHandlerNotConfigured(_serverConfig.Id, _serverConfig.Name);
                        throw new McpClientException("Roots handler not configured");
                    }
                    return await RootsHandler(request, _cts?.Token ?? CancellationToken.None);
                });
        }
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitializing)
        {
            _logger.ClientAlreadyInitializing(_serverConfig.Id, _serverConfig.Name);
            throw new InvalidOperationException("Client is already initializing");
        }
        _isInitializing = true;

        if (_isInitialized)
        {
            _logger.ClientAlreadyInitialized(_serverConfig.Id, _serverConfig.Name);
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
            _logger.ClientInitializationError(_serverConfig.Id, _serverConfig.Name, e);
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
            _logger.ServerCapabilitiesReceived(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(initializeResponse.Capabilities), JsonSerializer.Serialize(initializeResponse.ServerInfo));
            ServerCapabilities = initializeResponse.Capabilities;
            ServerInfo = initializeResponse.ServerInfo;

            // Validate protocol version
            if (initializeResponse.ProtocolVersion != _options.ProtocolVersion)
            {
                _logger.ServerProtocolVersionMismatch(_serverConfig.Id, _serverConfig.Name, _options.ProtocolVersion, initializeResponse.ProtocolVersion);
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
            _logger.ClientInitializationTimeout(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Initialization timed out");
        }
    }

    /// <inheritdoc/>
    public async Task PingAsync(CancellationToken cancellationToken)
    {
        _logger.PingingServer(_serverConfig.Id, _serverConfig.Name);
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
        _logger.ListingTools(_serverConfig.Id, _serverConfig.Name, cursor ?? "(null)");
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
        _logger.ListingPrompts(_serverConfig.Id, _serverConfig.Name, cursor ?? "(null)");
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
        _logger.GettingPrompt(_serverConfig.Id, _serverConfig.Name, name, arguments == null ? "{}" : JsonSerializer.Serialize(arguments));
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
        _logger.ListingResources(_serverConfig.Id, _serverConfig.Name, cursor ?? "(null)");
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
        _logger.ReadingResource(_serverConfig.Id, _serverConfig.Name, uri);
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
            _logger.InvalidCompletionReference(_serverConfig.Id, _serverConfig.Name, reference.ToString(), validationMessage);
            throw new McpClientException($"Invalid reference: {validationMessage}");
        }
        if (string.IsNullOrWhiteSpace(argumentName))
        {
            _logger.InvalidCompletionArgumentName(_serverConfig.Id, _serverConfig.Name, argumentName);
            throw new McpClientException("Argument name cannot be null or empty");
        }
        if (argumentValue is null)
        {
            _logger.InvalidCompletionArgumentValue(_serverConfig.Id, _serverConfig.Name, argumentValue);
            throw new McpClientException("Argument value cannot be null");
        }

        _logger.GettingCompletion(_serverConfig.Id, _serverConfig.Name, reference.ToString(), argumentName, argumentValue);
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
        _logger.SubscribingToResource(_serverConfig.Id, _serverConfig.Name, uri);
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
        _logger.UnsubscribingFromResource(_serverConfig.Id, _serverConfig.Name, uri);
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
        _logger.CallingTool(_serverConfig.Id, _serverConfig.Name, toolName, arguments == null ? "{}" : JsonSerializer.Serialize(arguments));
        return await SendRequestAsync<CallToolResponse>(
            new JsonRpcRequest
            {
                Method = "tools/call",
                Params = CreateParametersDictionary(toolName, arguments)
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal IMcpTransport Transport => _transport;

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
            _logger.ClientMessageProcessingCancelled(_serverConfig.Id, _serverConfig.Name);
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
                        _logger.RequestHandlerCalled(_serverConfig.Id, _serverConfig.Name, request.Method);    
                        var result = await handler(request);
                        _logger.RequestHandlerCompleted(_serverConfig.Id, _serverConfig.Name, request.Method);
                        await _transport.SendMessageAsync(new JsonRpcResponse
                        {
                            Id = request.Id,
                            JsonRpc = "2.0",
                            Result = result
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.RequestHandlerError(_serverConfig.Id, _serverConfig.Name, request.Method, ex);
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
                    _logger.NoRequestFoundForMessageWithId(_serverConfig.Id, _serverConfig.Name, messageWithId.Id.ToString());
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
                            _logger.NotificationHandlerError(_serverConfig.Id, _serverConfig.Name, notification.Method, ex);
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
            _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Transport is not connected");
        }

        // Set request ID
        request.Id = RequestId.FromNumber(Interlocked.Increment(ref _nextRequestId));

        var tcs = new TaskCompletionSource<IJsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.Id] = tcs;

        try
        {
            // Expensive logging, use the logging framework to check if the logger is enabled
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.SendingRequestPayload(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(request));
            }

            // Less expensive information logging
            _logger.SendingRequest(_serverConfig.Id, _serverConfig.Name, request.Method);

            await _transport.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
            var response = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (response is JsonRpcError error)
            {
                _logger.RequestFailed(_serverConfig.Id, _serverConfig.Name, request.Method, error.Error.Message, error.Error.Code);
                throw new McpClientException($"Request failed: {error.Error.Message}", error.Error.Code);
            }

            if (response is JsonRpcResponse success)
            {
                // Convert the Result object to JSON and back to get our strongly-typed result
                var resultJson = JsonSerializer.Serialize(success.Result, _jsonOptions);
                var resultObject = JsonSerializer.Deserialize<T>(resultJson, _jsonOptions);

                // Not expensive logging because we're already converting to JSON in order to get the result object
                _logger.RequestResponseReceivedPayload(_serverConfig.Id, _serverConfig.Name, resultJson);
                _logger.RequestResponseReceived(_serverConfig.Id, _serverConfig.Name, request.Method);

                if (resultObject != null)
                {
                    return resultObject;
                }

                // Result object was null, this is unexpected
                _logger.RequestResponseTypeConversionError(_serverConfig.Id, _serverConfig.Name, request.Method, typeof(T));
                throw new McpClientException($"Unexpected response type {JsonSerializer.Serialize(success.Result)}, expected {typeof(T)}");
            }

            // Unexpected response type
            _logger.RequestInvalidResponseType(_serverConfig.Id, _serverConfig.Name, request.Method);
            throw new McpClientException("Invalid response type");
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    /// <inheritdoc/>
    public async Task SendNotificationAsync(string method, CancellationToken cancellationToken = default)
    {
        if (!_transport.IsConnected)
        {
            _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Transport is not connected");
        }

        var notification = new JsonRpcNotification { Method = method };

        // Log if enabled
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.SendingNotificationPayload(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(notification));
        }

        // Log basic info
        _logger.SendingNotification(_serverConfig.Id, _serverConfig.Name, method);

        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task SendNotificationAsync<T>(string method, T parameters, CancellationToken cancellationToken = default)
    {
        if (!_transport.IsConnected)
        {
            _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Transport is not connected");
        }
        var notification = new JsonRpcNotification
        {
            Method = method,
            Params = parameters
        };
        // Log if enabled
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.SendingNotificationPayload(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(notification));
        }
        // Log basic info
        _logger.SendingNotification(_serverConfig.Id, _serverConfig.Name, method);
        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
        {
            _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
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

    public Func<CreateMessageRequestParams, CancellationToken, Task<CreateMessageResult>>? SamplingHandler { get; set; }

    public Func<ListRootsRequestParams, CancellationToken, Task<ListRootsResult>>? RootsHandler { get; set; }

    private void SetRequestHandler<TRequest, TResponse>(string method, Func<TRequest, Task<TResponse>> handler)
    {
        _requestHandlers[method] = async (request) => {
            // Convert the params JsonElement to our type using the same options
            var jsonString = JsonSerializer.Serialize(request.Params);
            var typedRequest = JsonSerializer.Deserialize<TRequest>(jsonString, _jsonOptions);

            if (typedRequest == null)
            {
                _logger.RequestParamsTypeConversionError(_serverConfig.Id, _serverConfig.Name, method, typeof(TRequest));
                throw new McpClientException($"Invalid request parameters type {jsonString}, expected {typeof(TRequest)}");
            }

            return await handler(typedRequest);
        };
    }

    private async Task CleanupAsync()
    {
        _logger.CleaningUpClient(_serverConfig.Id, _serverConfig.Name);

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

        _logger.ClientCleanedUp(_serverConfig.Id, _serverConfig.Name);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await CleanupAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}

