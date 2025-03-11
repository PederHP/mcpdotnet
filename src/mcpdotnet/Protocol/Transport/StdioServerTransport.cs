using System.Text.Json;
using McpDotNet.Logging;
using McpDotNet.Protocol.Messages;
using McpDotNet.Server;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Implements the MCP transport protocol over standard input/output streams.
/// </summary>
public sealed class StdioServerTransport : TransportBase, IServerTransport
{
    private readonly string _serverName;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private Task? _readTask;
    private CancellationTokenSource? _shutdownCts;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    private string EndpointName => $"Server (stdio) ({_serverName})";

    /// <summary>
    /// Initializes a new instance of the StdioServerTransport class.
    /// </summary>
    /// <param name="serverOptions">The server options.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    /// <param name="inputOutputStreams">Container for the input and output streams</param>
    public StdioServerTransport(McpServerOptions serverOptions, ILoggerFactory? loggerFactory, InputOutputStreams inputOutputStreams)
        : this(
              serverOptions is not null ? serverOptions.ServerInfo.Name : throw new ArgumentNullException(nameof(serverOptions)),
              loggerFactory,
              inputOutputStreams is not null ? inputOutputStreams.Output : throw new ArgumentNullException(nameof(inputOutputStreams)),
              inputOutputStreams.Input
              )
    {
    }

    /// <summary>
    /// Initializes a new instance of the StdioServerTransport class.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    public StdioServerTransport(string serverName, ILoggerFactory? loggerFactory)
        : this(serverName, loggerFactory, Console.Out, Console.In)
    {

    }

    /// <summary>
    /// Initializes a new instance of the StdioServerTransport class.
    /// </summary>
    /// <param name="serverName">The name of the server.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    /// <param name="output">The output stream writer</param>
    /// <param name="input">The input stream reader</param>
    public StdioServerTransport(string serverName, ILoggerFactory? loggerFactory, TextWriter output, TextReader input)
        : base(loggerFactory)
    {
        _serverName = serverName;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger<StdioClientTransport>() : NullLogger.Instance;
        _jsonOptions = JsonSerializerOptionsExtensions.DefaultOptions;
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <inheritdoc/>
    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        _shutdownCts = new CancellationTokenSource();

        _readTask = Task.Run(async () => await ReadMessagesAsync(_shutdownCts.Token).ConfigureAwait(false), CancellationToken.None);

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
        }

        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            _logger.TransportSendingMessage(EndpointName, id, json);

            await _output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            await _output.FlushAsync(cancellationToken).ConfigureAwait(false);

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
        await CleanupAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.TransportEnteringReadMessagesLoop(EndpointName);

            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.TransportWaitingForMessage(EndpointName);

                var reader = _input;
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    _logger.TransportEndOfStream(EndpointName);
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                _logger.TransportReceivedMessage(EndpointName, line);

                try
                {
                    var message = JsonSerializer.Deserialize<IJsonRpcMessage>(line, _jsonOptions);
                    if (message != null)
                    {
                        string messageId = "(no id)";
                        if (message is IJsonRpcMessageWithId messageWithId)
                        {
                            messageId = messageWithId.Id.ToString();
                        }
                        _logger.TransportReceivedMessageParsed(EndpointName, messageId);
                        await WriteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                        _logger.TransportMessageWritten(EndpointName, messageId);
                    }
                    else
                    {
                        _logger.TransportMessageParseUnexpectedType(EndpointName, line);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.TransportMessageParseFailed(EndpointName, line, ex);
                    // Continue reading even if we fail to parse a message
                }
            }
            _logger.TransportExitingReadMessagesLoop(EndpointName);
        }
        catch (OperationCanceledException)
        {
            _logger.TransportReadMessagesCancelled(EndpointName);
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.TransportReadMessagesFailed(EndpointName, ex);
        }
        finally
        {
            await CleanupAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        _logger.TransportCleaningUp(EndpointName);

        if (_shutdownCts != null)
        {
            await _shutdownCts.CancelAsync().ConfigureAwait(false);
            _shutdownCts.Dispose();
            _shutdownCts = null;
        }

        if (_readTask != null)
        {
            try
            {
                _logger.TransportWaitingForReadTask(EndpointName);
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.TransportCleanupReadTaskTimeout(EndpointName);
                // Continue with cleanup
            }
            catch (OperationCanceledException)
            {
                _logger.TransportCleanupReadTaskCancelled(EndpointName);
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.TransportCleanupReadTaskFailed(EndpointName, ex);
            }
            _readTask = null;
            _logger.TransportReadTaskCleanedUp(EndpointName);
        }

        SetConnected(false);
        _logger.TransportCleanedUp(EndpointName);
    }
}