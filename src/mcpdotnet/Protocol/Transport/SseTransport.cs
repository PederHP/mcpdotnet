﻿
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
public sealed class SseTransport : TransportBase
{
    private readonly HttpClient _httpClient;
    private readonly SseTransportOptions _options;
    private readonly Uri _sseEndpoint;
    private Uri? _messageEndpoint;
    private readonly CancellationTokenSource _connectionCts;
    private Task? _receiveTask;
    private readonly ILogger<SseTransport> _logger;
    private readonly McpServerConfig _serverConfig;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="transportOptions"></param>
    /// <param name="serverConfig"></param>
    /// <param name="loggerFactory"></param>
    public SseTransport(SseTransportOptions transportOptions, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
    {
        _options = transportOptions;
        _serverConfig = serverConfig;
        _sseEndpoint = new Uri(serverConfig.Location!);
        _httpClient = new HttpClient();
        _connectionCts = new CancellationTokenSource();
        _logger = loggerFactory.CreateLogger<SseTransport>();
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
    }

    /// <inheritdoc/>
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsConnected)
            {
                _logger.TransportAlreadyConnected(_serverConfig.Id, _serverConfig.Name);
                throw new McpTransportException("Transport is already connected");
            }

            // Start message receiving loop
            _receiveTask = ReceiveMessagesAsync(_connectionCts.Token);

            _logger.TransportReadingMessages(_serverConfig.Id, _serverConfig.Name);

            await Task.WhenAny(
                Task.Delay(_options.ConnectionTimeout, cancellationToken),
                Task.Run(() => IsConnected)
            );

            if (!IsConnected)
                throw new TimeoutException("Failed to receive endpoint event");
        }
        catch (Exception ex)
        {
            _logger.TransportConnectFailed(_serverConfig.Id, _serverConfig.Name, ex);
            await CloseAsync();
            throw new McpTransportException("Failed to connect transport", ex);
        }
    }

    public override async Task SendMessageAsync(
        IJsonRpcMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_messageEndpoint == null)
            throw new InvalidOperationException("Transport not connected");

        var content = new StringContent(
            JsonSerializer.Serialize(message),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(
            _messageEndpoint,
            content,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _sseEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

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
                _logger.TransportConnectionError(_serverConfig.Id, _serverConfig.Name, ex);
                await Task.Delay(_options.ReconnectDelay, cancellationToken);
            }
        }
    }

    private async Task ProcessSseMessage(string data, CancellationToken cancellationToken)
    {
        if (!IsConnected)
        {
            _logger.TransportMessageReceivedBeforeConnected(_serverConfig.Id, _serverConfig.Name, data);
            return;
        }

        try
        {
            var message = JsonSerializer.Deserialize<IJsonRpcMessage>(data, _jsonOptions);
            if (message == null)
            {
                _logger.TransportMessageParseUnexpectedType(_serverConfig.Id, _serverConfig.Name, data);
                return;
            }

            string messageId = "(no id)";
            if (message is IJsonRpcMessageWithId messageWithId)
            {
                messageId = messageWithId.Id.ToString();
            }

            _logger.TransportReceivedMessageParsed(_serverConfig.Id, _serverConfig.Name, messageId);
            await WriteMessageAsync(message, cancellationToken);
            _logger.TransportMessageWritten(_serverConfig.Id, _serverConfig.Name, messageId);
        }
        catch (JsonException ex)
        {
            _logger.TransportMessageParseFailed(_serverConfig.Id, _serverConfig.Name, data, ex);
        }
    }

    private void HandleEndpointEvent(string data)
    {
        try
        {
            var endpointData = JsonSerializer.Deserialize<EndpointEventData>(data, _jsonOptions);
            if (endpointData?.Uri == null)
            {
                _logger.TransportEndpointEventInvalid(_serverConfig.Id, _serverConfig.Name, data);
                return;
            }

            _messageEndpoint = new Uri(endpointData.Uri);
            SetConnected(true);
        }
        catch (JsonException ex)
        {
            _logger.TransportEndpointEventParseFailed(_serverConfig.Id, _serverConfig.Name, data, ex);
            throw new McpTransportException("Failed to parse endpoint event", ex);
        }
    }

    private record EndpointEventData
    {
        [JsonPropertyName("uri")]
        public string? Uri { get; init; }
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
}