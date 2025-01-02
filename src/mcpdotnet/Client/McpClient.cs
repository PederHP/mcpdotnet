namespace McpDotNet.Client;

using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Transport;
using System.Collections.Concurrent;
using McpDotNet.Protocol.Messages;
using System.Diagnostics;
using System.Text.Json;
using McpDotNet.Utils.Json;

/// </inheritdoc>
internal class McpClient : IMcpClient
{
    private readonly IMcpTransport _transport;
    private readonly McpClientOptions _options;
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<IJsonRpcMessage>> _pendingRequests;
    private readonly ConcurrentDictionary<string, List<Func<JsonRpcNotification,Task>>> _notificationHandlers;
    private int _nextRequestId;
    private bool _isInitialized;
    private Task? _messageProcessingTask;
    private CancellationTokenSource? _cts;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions().ConfigureForMcp();

    /// </inheritdoc>
    public bool IsInitialized => _isInitialized;

    /// </inheritdoc>
    public ServerCapabilities? ServerCapabilities { get; private set; }

    /// </inheritdoc>
    public Implementation? ServerInfo { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="McpClient"/> class.
    /// </summary>
    /// <param name="transport">An MCP transport implementation.</param>
    /// <param name="options">Options for the client, defining protocol version and capabilities.</param>
    public McpClient(IMcpTransport transport, McpClientOptions options)
    {
        _transport = transport;
        _options = options;
        _pendingRequests = new();
        _notificationHandlers = new();
        _nextRequestId = 1;
    }

    /// </inheritdoc>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Client is already initialized");
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
            Debug.WriteLine($"Client connection error: {e}");
            await CleanupAsync().ConfigureAwait(false);
            throw;
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
            ServerCapabilities = initializeResponse.Capabilities;
            ServerInfo = initializeResponse.ServerInfo;

            // Validate protocol version
            if (initializeResponse.ProtocolVersion != _options.ProtocolVersion)
            {
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
            throw new McpClientException("Initialization timed out");
        }
    }

    /// </inheritdoc>
    public async Task PingAsync(CancellationToken cancellationToken)
    {
        await SendRequestAsync<dynamic>(
            new JsonRpcRequest
            {
                Method = "ping"
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// </inheritdoc>
    public async Task<ListToolsResponse> ListToolsAsync(CancellationToken cancellationToken)
    {
        return await SendRequestAsync<ListToolsResponse>(
            new JsonRpcRequest
            {
                Method = "tools/list",
                // No parameters needed for tools/list
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// </inheritdoc>
    public async Task<CallToolResponse> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<CallToolResponse>(
            new JsonRpcRequest
            {
                Method = "tools/call",
                Params = new Dictionary<string, object?>
                {
                    ["name"] = toolName,
                    ["arguments"] = arguments
                }
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _transport.MessageReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await HandleMessageAsync(message).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (NullReferenceException)
        {
            // Ignore reader disposal and mocked transport
        }
    }

    private async Task HandleMessageAsync(IJsonRpcMessage message)
    {
        switch (message)
        {
            case IJsonRpcMessageWithId messageWithId:
                if (_pendingRequests.TryRemove(messageWithId.Id, out var tcs))
                {
                    tcs.TrySetResult(message);
                }
                break;

            case JsonRpcNotification notification:
                if (_notificationHandlers.TryGetValue(notification.Method, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        try
                        {
                            await handler(notification).ConfigureAwait(false);
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

    /// </inheritdoc>
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

    /// </inheritdoc>
    public void OnNotification(string method, Func<JsonRpcNotification,Task> handler)
    {
        var handlers = _notificationHandlers.GetOrAdd(method, _ => new());
        lock (handlers)
        {
            handlers.Add(handler);
        }
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
