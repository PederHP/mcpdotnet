
using System.Diagnostics.Tracing;
using System.Text.Json;
using System.Text;
using McpDotNet.Protocol.Messages;
using McpDotNet.Configuration;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using McpDotNet.Utils.Json;
using System.Threading;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// 
/// </summary>
public sealed class SseClientTransport : TransportBase, IClientTransport
{
    private readonly HttpClient _httpClient;
    private readonly SseClientTransportOptions _options;
    private readonly Uri _sseEndpoint;
    private Uri? _messageEndpoint;
    private readonly CancellationTokenSource _connectionCts;
    private Task? _receiveTask;
    private readonly ILogger<SseClientTransport> _logger;
    private readonly McpServerConfig _serverConfig;
    private readonly JsonSerializerOptions _jsonOptions;
    private TaskCompletionSource<bool> _connectionEstablished;

    private string EndpointName => $"Client (SSE) for ({_serverConfig.Id}: {_serverConfig.Name})";

    /// <summary>
    /// SSE transport for client endpoints. Unlike stdio it does not launch a process, but connects to an existing server.
    /// The HTTP server can be local or remote, and must support the SSE protocol.
    /// </summary>
    /// <param name="transportOptions">Configuration options for the transport.</param>
    /// <param name="serverConfig">The configuration object indicating which server to connect to.</param>
    /// <param name="loggerFactory">Logger factory for creating loggers.</param>
    public SseClientTransport(SseClientTransportOptions transportOptions, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _options = transportOptions;
        _serverConfig = serverConfig;
        _sseEndpoint = new Uri(serverConfig.Location!);
        _httpClient = new HttpClient();
        _connectionCts = new CancellationTokenSource();
        _logger = loggerFactory.CreateLogger<SseClientTransport>();
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
        _connectionEstablished = new TaskCompletionSource<bool>();
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsConnected)
            {
                _logger.TransportAlreadyConnected(EndpointName);
                throw new McpTransportException("Transport is already connected");
            }

            // Start message receiving loop
            _receiveTask = ReceiveMessagesAsync(_connectionCts.Token);

            _logger.TransportReadingMessages(EndpointName);

            await Task.WhenAny(
                _connectionEstablished.Task,
                Task.Delay(_options.ConnectionTimeout, cancellationToken)
            );

            if (!IsConnected)
                throw new TimeoutException("Failed to receive endpoint event");
        }
        catch (McpTransportException)
        {
            // Rethrow transport exceptions
            throw;
        }
        catch (Exception ex)
        {
            _logger.TransportConnectFailed(EndpointName, ex);
            await CloseAsync();
            throw new McpTransportException("Failed to connect transport", ex);
        }
    }

    /// <inheritdoc/>
    public override async Task SendMessageAsync(
        IJsonRpcMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_messageEndpoint == null)
            throw new InvalidOperationException("Transport not connected");

        var content = new StringContent(
            JsonSerializer.Serialize(message, _jsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            _messageEndpoint,
            content,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();


        var responseContent = await response.Content.ReadAsStringAsync();

        // Handle notifications, which don't have responses
        if (message is JsonRpcNotification)
        {
            await HandleNotificationResponseAsync(message, responseContent, cancellationToken);
            return;
        }

        var responseMessage = JsonSerializer.Deserialize<IJsonRpcMessage>(responseContent, _jsonOptions);
        if (responseMessage != null)
        {
            string messageId = "(no id)";
            if (responseMessage is IJsonRpcMessageWithId messageWithId)
            {
                messageId = messageWithId.Id.ToString();
            }
            _logger.TransportReceivedMessageParsed(EndpointName, messageId);
            await WriteMessageAsync(responseMessage, cancellationToken);
            _logger.TransportMessageWritten(EndpointName, messageId);
        }
        else
        {
            _logger.TransportMessageParseUnexpectedType(EndpointName, responseContent);
        }
    }

    /// <inheritdoc/>
    public async Task CloseAsync()
    {
        _connectionCts.Cancel();
        if (_receiveTask != null)
            await _receiveTask;

        _httpClient.Dispose();
        _connectionCts.Dispose();
        SetConnected(false);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await CloseAsync();
        GC.SuppressFinalize(this);
    }

    private async Task HandleNotificationResponseAsync(
        IJsonRpcMessage message,
        string responseContent,
        CancellationToken cancellationToken)
    {
        // Check for error response, but don't require one
        try
        {
            var errorResponse = JsonSerializer.Deserialize<JsonRpcError>(responseContent, _jsonOptions);
            if (errorResponse is not null)
            {
                await WriteMessageAsync(errorResponse, cancellationToken);
                return;
            }
        }
        catch (JsonException)
        {
            // Ignore deserialization errors for notifications
            // This is expected as most notifications won't have responses
        }
    }

    internal Uri? MessageEndpoint => _messageEndpoint;

    internal SseClientTransportOptions Options => _options;

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        int reconnectAttempts = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _sseEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                // Reset reconnect attempts on successful connection
                reconnectAttempts = 0;

                string? currentEvent = null;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("event: "))
                    {
                        currentEvent = line.Substring(7);
                    }
                    else if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (currentEvent == "endpoint")
                        {
                            HandleEndpointEvent(data);
                        }
                        else
                        {
                            await ProcessSseMessage(data, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.TransportConnectionError(EndpointName, ex);

                reconnectAttempts++;
                if (reconnectAttempts >= _options.MaxReconnectAttempts)
                {
                    throw new McpTransportException("Exceeded reconnect limit", ex);
                }

                await Task.Delay(_options.ReconnectDelay, cancellationToken);
            }
        }
    }

    private async Task ProcessSseMessage(string data, CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            _logger.TransportMessageReceivedBeforeConnected(EndpointName, data);
            return;
        }

        try
        {
            var message = JsonSerializer.Deserialize<IJsonRpcMessage>(data, _jsonOptions);
            if (message == null)
            {
                _logger.TransportMessageParseUnexpectedType(EndpointName, data);
                return;
            }

            string messageId = "(no id)";
            if (message is IJsonRpcMessageWithId messageWithId)
            {
                messageId = messageWithId.Id.ToString();
            }

            _logger.TransportReceivedMessageParsed(EndpointName, messageId);
            await WriteMessageAsync(message, cancellationToken);
            _logger.TransportMessageWritten(EndpointName, messageId);
        }
        catch (JsonException ex)
        {
            _logger.TransportMessageParseFailed(EndpointName, data, ex);
        }
    }

    private void HandleEndpointEvent(string data)
    {
        try
        {
            var endpointData = JsonSerializer.Deserialize<EndpointEventData>(data, _jsonOptions);
            if (endpointData?.Uri == null)
            {
                _logger.TransportEndpointEventInvalid(EndpointName, data);
                return;
            }

            _messageEndpoint = new Uri(endpointData.Uri);
            SetConnected(true);
            _connectionEstablished.TrySetResult(true);
        }
        catch (JsonException ex)
        {
            _logger.TransportEndpointEventParseFailed(EndpointName, data, ex);
            throw new McpTransportException("Failed to parse endpoint event", ex);
        }
    }

    private record EndpointEventData
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; init; }
    }
}