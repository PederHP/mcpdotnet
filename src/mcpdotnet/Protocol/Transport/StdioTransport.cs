using System.Diagnostics;
using System.Text.Json;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging;
using McpDotNet.Logging;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Implements the MCP transport protocol over standard input/output streams.
/// </summary>
public sealed class StdioTransport : TransportBase
{
    private readonly StdioTransportOptions _options;
    private readonly McpServerConfig _serverConfig;
    private readonly ILogger<StdioTransport> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private Process? _process;
    private Task? _readTask;
    private CancellationTokenSource? _shutdownCts;
    private bool _processStarted;

    /// <summary>
    /// Initializes a new instance of the StdioTransport class.
    /// </summary>
    /// <param name="options">Configuration options for the transport.</param>
    /// <param name="serverConfig">The server configuration for the transport.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    public StdioTransport(StdioTransportOptions options, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
    {
        _options = options;
        _serverConfig = serverConfig;
        _logger = loggerFactory.CreateLogger<StdioTransport>();
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp(loggerFactory);
    }

    /// <inheritdoc/>
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.TransportAlreadyConnected(_serverConfig.Id, _serverConfig.Name);
            throw new McpTransportException("Transport is already connected");
        }

        try
        {
            _logger.TransportConnecting(_serverConfig.Id, _serverConfig.Name);

            _shutdownCts = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.Command,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _options.WorkingDirectory ?? Environment.CurrentDirectory,
            };

            if (_options.Arguments != null)
            {
                foreach (var arg in _options.Arguments)
                {
                    startInfo.ArgumentList.Add(arg);
                }
            }

            if (_options.EnvironmentVariables != null)
            {
                foreach (var (key, value) in _options.EnvironmentVariables)
                {
                    startInfo.Environment[key] = value;
                }
            }

            _logger.CreateProcessForTransport(_serverConfig.Id, _serverConfig.Name, _options.Command,
                string.Join(", ", startInfo.ArgumentList), string.Join(", ", startInfo.Environment.Select(kvp => kvp.Key + "=" + kvp.Value)),
                startInfo.WorkingDirectory, _options.ShutdownTimeout.ToString());

            _process = new Process { StartInfo = startInfo };

            // Set up error logging
            _process.ErrorDataReceived += (sender, args) =>
            {
                _logger.TransportError(_serverConfig.Id, _serverConfig.Name, args.Data ?? "(no data)");
            };

            if (!_process.Start())
            {
                _logger.TransportProcessStartFailed(_serverConfig.Id, _serverConfig.Name);
                throw new McpTransportException("Failed to start MCP server process");
            }
            _logger.TransportProcessStarted(_serverConfig.Id, _serverConfig.Name, _process.Id);
            _processStarted = true;
            _process.BeginErrorReadLine();


            // Start reading messages in the background
            _readTask = Task.Run(async () => await ReadMessagesAsync(_shutdownCts.Token));
            _logger.TransportReadingMessages(_serverConfig.Id, _serverConfig.Name);

            SetConnected(true);
        }
        catch (Exception ex)
        {
            _logger.TransportConnectFailed(_serverConfig.Id, _serverConfig.Name, ex);
            await CleanupAsync(cancellationToken);
            throw new McpTransportException("Failed to connect transport", ex);
        }
    }

    /// <inheritdoc/>
    public override async Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _process?.HasExited == true)
        {
            _logger.TransportNotConnected(_serverConfig.Id, _serverConfig.Name);
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
            _logger.TransportSendingMessage(_serverConfig.Id, _serverConfig.Name, id, json);

            // Write the message followed by a newline
            await _process!.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync();

            _logger.TransportSentMessage(_serverConfig.Id, _serverConfig.Name, id);
        }
        catch (Exception ex)
        {
            _logger.TransportSendFailed(_serverConfig.Id, _serverConfig.Name, id, ex);
            throw new McpTransportException("Failed to send message", ex);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await CleanupAsync(CancellationToken.None);
        GC.SuppressFinalize(this);
    }

    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.TransportEnteringReadMessagesLoop(_serverConfig.Id, _serverConfig.Name);

            using var reader = _process!.StandardOutput;

            while (!cancellationToken.IsCancellationRequested && !_process.HasExited)
            {
                _logger.TransportWaitingForMessage(_serverConfig.Id, _serverConfig.Name);
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    _logger.TransportEndOfStream(_serverConfig.Id, _serverConfig.Name);
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                _logger.TransportReceivedMessage(_serverConfig.Id, _serverConfig.Name, line);

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
                        _logger.TransportReceivedMessageParsed(_serverConfig.Id, _serverConfig.Name, messageId);
                        await WriteMessageAsync(message, cancellationToken);
                        _logger.TransportMessageWritten(_serverConfig.Id, _serverConfig.Name, messageId);
                    }
                    else
                    {
                        _logger.TransportMessageParseUnexpectedType(_serverConfig.Id, _serverConfig.Name, line);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.TransportMessageParseFailed(_serverConfig.Id, _serverConfig.Name, line, ex);
                    // Continue reading even if we fail to parse a message
                }
            }
            _logger.TransportExitingReadMessagesLoop(_serverConfig.Id, _serverConfig.Name);
        }
        catch (OperationCanceledException)
        {
            _logger.TransportReadMessagesCancelled(_serverConfig.Id, _serverConfig.Name);
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.TransportReadMessagesFailed(_serverConfig.Id, _serverConfig.Name, ex);
        }
        finally
        {
            await CleanupAsync(cancellationToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        _logger.TransportCleaningUp(_serverConfig.Id, _serverConfig.Name);
        if (_process != null && _processStarted && !_process.HasExited)
        {            
            try
            {
                // Try to close stdin to signal the process to exit
                _logger.TransportClosingStdin(_serverConfig.Id, _serverConfig.Name);
                _process.StandardInput.Close();

                // Wait for the process to exit
                _logger.TransportWaitingForShutdown(_serverConfig.Id, _serverConfig.Name);
                if (!_process.WaitForExit((int)_options.ShutdownTimeout.TotalMilliseconds))
                {
                    // If it doesn't exit gracefully, terminate it
                    _logger.TransportKillingProcess(_serverConfig.Id, _serverConfig.Name);
                    _process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                _logger.TransportShutdownFailed(_serverConfig.Id, _serverConfig.Name, ex);
            }

            _process.Dispose();
            _process = null;
        }

        _shutdownCts?.Cancel();
        _shutdownCts?.Dispose();
        _shutdownCts = null;

        if (_readTask != null)
        {
            try
            {
                _logger.TransportWaitingForReadTask(_serverConfig.Id, _serverConfig.Name);
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.TransportCleanupReadTaskTimeout(_serverConfig.Id, _serverConfig.Name);
                // Continue with cleanup
            }
            catch (OperationCanceledException)
            {
                _logger.TransportCleanupReadTaskCancelled(_serverConfig.Id, _serverConfig.Name);
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.TransportCleanupReadTaskFailed(_serverConfig.Id, _serverConfig.Name, ex);
            }
            _readTask = null;
            _logger.TransportReadTaskCleanedUp(_serverConfig.Id, _serverConfig.Name);
        }

        SetConnected(false);
        _logger.TransportCleanedUp(_serverConfig.Id, _serverConfig.Name);
    }

}