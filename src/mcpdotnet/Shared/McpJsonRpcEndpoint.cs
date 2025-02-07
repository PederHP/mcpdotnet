using McpDotNet.Client;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace McpDotNet.Shared;

internal abstract class McpJsonRpcEndpoint
{
    private readonly ITransport _transport;
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<IJsonRpcMessage>> _pendingRequests;
    private readonly ConcurrentDictionary<string, List<Func<JsonRpcNotification, Task>>> _notificationHandlers;
    private readonly Dictionary<string, Func<JsonRpcRequest, Task<object>>> _requestHandlers = new();
    private int _nextRequestId;
    private Task? _messageProcessingTask;
    private CancellationTokenSource? _cts;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<McpClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpJsonRpcEndpoint"/> class.
    /// </summary>
    /// <param name="transport">An MCP transport implementation.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public McpJsonRpcEndpoint(ITransport transport, ILoggerFactory loggerFactory)
    {
        _transport = transport;
        _pendingRequests = new();
        _notificationHandlers = new();
        _nextRequestId = 1;
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
        _logger = loggerFactory.CreateLogger<McpClient>();
    }

    public bool IsInitialized { get; set; }

    public abstract string EndpointName { get; }

    internal ITransport Transport => _transport;

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _transport.MessageReader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                _logger.TransportMessageRead(EndpointName, message.GetType().Name);
                // Fire and forget the message handling task to avoid blocking the transport
                // If awaiting the task, the transport will not be able to read more messages,
                // which could lead to a deadlock if the handler sends a message back
                FireAndForget(Task.Run(() => HandleMessageAsync(message, cancellationToken)), message);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
            _logger.EndpointMessageProcessingCancelled(EndpointName);
        }
        catch (NullReferenceException)
        {
            // Ignore reader disposal and mocked transport
        }
    }

    private void FireAndForget(Task task, IJsonRpcMessage message)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                var payload = JsonSerializer.Serialize(message);
                _logger.MessageHandlerError(EndpointName, message.GetType().Name, payload, t.Exception);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
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
                        _logger.RequestHandlerCalled(EndpointName, request.Method);
                        var result = await handler(request);
                        _logger.RequestHandlerCompleted(EndpointName, request.Method);
                        await _transport.SendMessageAsync(new JsonRpcResponse
                        {
                            Id = request.Id,
                            JsonRpc = "2.0",
                            Result = result
                        }, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.RequestHandlerError(EndpointName, request.Method, ex);
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
                else
                {
                    _logger.NoHandlerFoundForRequest(EndpointName, request.Method);
                }
                break;
            case IJsonRpcMessageWithId messageWithId:
                if (_pendingRequests.TryRemove(messageWithId.Id, out var tcs))
                {
                    _logger.ResponseMatchedPendingRequest(EndpointName, messageWithId.Id.ToString());
                    tcs.TrySetResult(message);
                }
                else
                {
                    _logger.NoRequestFoundForMessageWithId(EndpointName, messageWithId.Id.ToString());
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
                            _logger.NotificationHandlerError(EndpointName, notification.Method, ex);
                        }
                    }
                }
                break;

            default:
                _logger.EndpointHandlerUnexpectedMessageType(EndpointName, message.GetType().Name);
                break;
        }
    }

    public async Task<T> SendRequestAsync<T>(JsonRpcRequest request, CancellationToken cancellationToken) where T : class
    {
        if (!_transport.IsConnected)
        {
            _logger.EndpointNotConnected(EndpointName);
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
                _logger.SendingRequestPayload(EndpointName, JsonSerializer.Serialize(request));
            }

            // Less expensive information logging
            _logger.SendingRequest(EndpointName, request.Method);

            await _transport.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);

            _logger.RequestSentAwaitingResponse(EndpointName, request.Method, request.Id.ToString());
            var response = await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (response is JsonRpcError error)
            {
                _logger.RequestFailed(EndpointName, request.Method, error.Error.Message, error.Error.Code);
                throw new McpClientException($"Request failed (server side): {error.Error.Message}", error.Error.Code);
            }

            if (response is JsonRpcResponse success)
            {
                // Convert the Result object to JSON and back to get our strongly-typed result
                var resultJson = JsonSerializer.Serialize(success.Result, _jsonOptions);
                var resultObject = JsonSerializer.Deserialize<T>(resultJson, _jsonOptions);

                // Not expensive logging because we're already converting to JSON in order to get the result object
                _logger.RequestResponseReceivedPayload(EndpointName, resultJson);
                _logger.RequestResponseReceived(EndpointName, request.Method);

                if (resultObject != null)
                {
                    return resultObject;
                }

                // Result object was null, this is unexpected
                _logger.RequestResponseTypeConversionError(EndpointName, request.Method, typeof(T));
                throw new McpClientException($"Unexpected response type {JsonSerializer.Serialize(success.Result)}, expected {typeof(T)}");
            }

            // Unexpected response type
            _logger.RequestInvalidResponseType(EndpointName, request.Method);
            throw new McpClientException("Invalid response type");
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    public async Task SendNotificationAsync(string method, CancellationToken cancellationToken = default)
    {
        if (!_transport.IsConnected)
        {
            // TODO: _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Transport is not connected");
        }

        var notification = new JsonRpcNotification { Method = method };

        // Log if enabled
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            // TODO: _logger.SendingNotificationPayload(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(notification));
        }

        // Log basic info
        // TODO: _logger.SendingNotification(_serverConfig.Id, _serverConfig.Name, method);

        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendNotificationAsync<T>(string method, T parameters, CancellationToken cancellationToken = default)
    {
        if (!_transport.IsConnected)
        {
            // TODO: _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
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
            // TODO: _logger.SendingNotificationPayload(_serverConfig.Id, _serverConfig.Name, JsonSerializer.Serialize(notification));
        }
        // Log basic info
        // TODO: _logger.SendingNotification(_serverConfig.Id, _serverConfig.Name, method);
        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
        {
            // TODO: _logger.ClientNotConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpClientException("Transport is not connected");
        }

        await _transport.SendMessageAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    public void OnNotification(string method, Func<JsonRpcNotification, Task> handler)
    {
        var handlers = _notificationHandlers.GetOrAdd(method, _ => new());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected void SetRequestHandler<TRequest, TResponse>(string method, Func<TRequest, Task<TResponse>> handler)
    {
        _requestHandlers[method] = async (request) =>
        {
            // Convert the params JsonElement to our type using the same options
            var jsonString = JsonSerializer.Serialize(request.Params);
            var typedRequest = JsonSerializer.Deserialize<TRequest>(jsonString, _jsonOptions);

            if (typedRequest == null)
            {
                typedRequest = default;

                // TODO: Add a check that the arguments are all optional - use an attribute on the request type?
                //_logger.RequestParamsTypeConversionError(EndpointName, method, typeof(TRequest));
                //throw new McpClientException($"Invalid request parameters type {jsonString}, expected {typeof(TRequest)}");
            }

            return await handler(typedRequest);
        };
    }

    protected Task? MessageProcessingTask
    {
        get => _messageProcessingTask;
        set => _messageProcessingTask = value;
    }

    protected CancellationTokenSource? CancellationTokenSource
    {
        get => _cts;
        set => _cts = value;
    }

    protected async Task CleanupAsync()
    {
        // TODO: _logger.CleaningUpClient(_serverConfig.Id, _serverConfig.Name);

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

        IsInitialized = false;

        // TODO: _logger.ClientCleanedUp(_serverConfig.Id, _serverConfig.Name);
    }
}
