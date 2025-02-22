using System.Text.Json;
using Microsoft.Extensions.Logging;
using McpDotNet.Protocol.Messages;
using McpDotNet.Utils.Json;
using McpDotNet.Logging;
using System.Collections.Concurrent;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Implements the MCP transport protocol over standard input/output streams.
/// </summary>
public sealed class SseServerTransport : TransportBase, IServerTransport
{
    private readonly string _serverName;
    private readonly IHttpServerProvider _httpServerProvider;
    private readonly ILogger<SseServerTransport> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private CancellationTokenSource? _shutdownCts;
    private readonly ConcurrentDictionary<RequestId, TaskCompletionSource<IJsonRpcMessage>> _pendingRequests;

    private string EndpointName => $"Server (SSE) ({_serverName})";

    /// <summary>
    /// Initializes a new instance of the SseServerTransport class.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="httpServerProvider">An HTTP server provider for handling HTTP requests and SSE.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    public SseServerTransport(string serverName, IHttpServerProvider httpServerProvider, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _serverName = serverName;
        _httpServerProvider = httpServerProvider;
        _logger = loggerFactory.CreateLogger<SseServerTransport>();
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
        _pendingRequests = new ConcurrentDictionary<RequestId, TaskCompletionSource<IJsonRpcMessage>>();
    }

    /// <inheritdoc/>
    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _shutdownCts = new CancellationTokenSource();

        _httpServerProvider.InitializeMessageHandler(HttpMessageHandler);
        _httpServerProvider.StartAsync(cancellationToken);

        SetConnected(true);

        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    public override async Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.TransportNotConnected(EndpointName);
            throw new McpTransportException("Transport is not connected");
        }

        string id = "(no id)";
        if (message is IJsonRpcMessageWithId messageWithId)
        {
            id = messageWithId.Id.ToString();

            // If the message has an ID and is a response, it might be the response to a request
            if (message is JsonRpcResponse response)
            {
                if (_pendingRequests.TryGetValue(messageWithId.Id, out var tcs))
                {
                    tcs.SetResult(response);
                    return;
                }
                _logger.NoRequestFoundForMessageWithId(EndpointName, id);
            }
        }

        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            _logger.TransportSendingMessage(EndpointName, id, json);

            await _httpServerProvider.SendEvent(json, "message");

            _logger.TransportSentMessage(EndpointName, id);
        }
        catch (Exception ex)
        {
            _logger.TransportSendFailed(EndpointName, id, ex);
            throw new McpTransportException("Failed to send message", ex);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await CleanupAsync(CancellationToken.None);
        GC.SuppressFinalize(this);
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        _logger.TransportCleaningUp(EndpointName);

        _shutdownCts?.Cancel();
        _shutdownCts?.Dispose();
        _shutdownCts = null;

        // TODO: Cleanup SSE server transport

        SetConnected(false);
        _logger.TransportCleanedUp(EndpointName);
    }

    private async Task<string> HttpMessageHandler(string request, CancellationToken cancellationToken)
    {
        _logger.TransportReceivedMessage(EndpointName, request);

        try
        {
            var message = JsonSerializer.Deserialize<IJsonRpcMessage>(request, _jsonOptions);
            if (message != null)
            {
                string messageId = "(no id)";
                if (message is IJsonRpcMessageWithId messageWithId)
                {
                    messageId = messageWithId.Id.ToString();
                    var tcs = new TaskCompletionSource<IJsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pendingRequests.TryAdd(messageWithId.Id, tcs);

                    _logger.TransportReceivedMessageParsed(EndpointName, messageId);
                    await WriteMessageAsync(message, cancellationToken);
                    _logger.TransportMessageWritten(EndpointName, messageId);

                    var response = await tcs.Task;
                    _pendingRequests.TryRemove(messageWithId.Id, out _);
                    return JsonSerializer.Serialize(response, _jsonOptions);
                }
                else
                {
                    // No id, just write an empty response
                    _logger.TransportReceivedMessageParsed(EndpointName, messageId);
                    await WriteMessageAsync(message, cancellationToken);
                    _logger.TransportMessageWritten(EndpointName, messageId);
                    return JsonSerializer.Serialize(new { jsonrpc = "2.0" }); 
                }
            }
            else
            {
                _logger.TransportMessageParseUnexpectedType(EndpointName, request);
                // TODO: proper error response
                return JsonSerializer.Serialize(new { jsonrpc = "2.0" });
            }
        }
        catch (JsonException ex)
        {
            _logger.TransportMessageParseFailed(EndpointName, request, ex);
            // TODO: proper error response
            return JsonSerializer.Serialize(new { jsonrpc = "2.0" });
        }
    }
}
