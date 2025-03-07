using System.Diagnostics;
using System.Text.Json;
using McpDotNet.Configuration;
using McpDotNet.Logging;
using McpDotNet.Protocol.Messages;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Implements the MCP transport protocol over standard input/output streams.
/// </summary>
public sealed class StdioClientTransport : TransportBase, IClientTransport
{
    private readonly StdioClientTransportOptions _options;
    private readonly McpServerConfig _serverConfig;
    private readonly ILogger<StdioClientTransport> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private Process? _process;
    private Task? _readTask;
    private CancellationTokenSource? _shutdownCts;
    private bool _processStarted;

    private string EndpointName => $"Client (stdio) for ({_serverConfig.Id}: {_serverConfig.Name})";

    /// <summary>
    /// Initializes a new instance of the StdioTransport class.
    /// </summary>
    /// <param name="options">Configuration options for the transport.</param>
    /// <param name="serverConfig">The server configuration for the transport.</param>
    /// <param name="loggerFactory">A logger factory for creating loggers.</param>
    public StdioClientTransport(StdioClientTransportOptions options, McpServerConfig serverConfig, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _options = options;
        _serverConfig = serverConfig;
        _logger = loggerFactory.CreateLogger<StdioClientTransport>();
        _jsonOptions = JsonSerializerOptionsExtensions.DefaultOptions;
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.TransportAlreadyConnected(EndpointName);
            throw new McpTransportException("Transport is already connected");
        }

        try
        {
            _logger.TransportConnecting(EndpointName);

            _shutdownCts = new CancellationTokenSource();

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.Command,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = OperatingSystem.IsWindows(),
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

            _logger.CreateProcessForTransport(EndpointName, _options.Command,
                string.Join(", ", startInfo.ArgumentList), string.Join(", ", startInfo.Environment.Select(kvp => kvp.Key + "=" + kvp.Value)),
                startInfo.WorkingDirectory, _options.ShutdownTimeout.ToString());

            _process = new Process { StartInfo = startInfo };

            // Set up error logging
            _process.ErrorDataReceived += (sender, args) =>
            {
                _logger.TransportError(EndpointName, args.Data ?? "(no data)");
            };

            if (!_process.Start())
            {
                _logger.TransportProcessStartFailed(EndpointName);
                throw new McpTransportException("Failed to start MCP server process");
            }
            _logger.TransportProcessStarted(EndpointName, _process.Id);
            _processStarted = true;
            _process.BeginErrorReadLine();


            // Start reading messages in the background
            _readTask = Task.Run(async () => await ReadMessagesAsync(_shutdownCts.Token));
            _logger.TransportReadingMessages(EndpointName);

            SetConnected(true);
        }
        catch (Exception ex)
        {
            _logger.TransportConnectFailed(EndpointName, ex);
            await CleanupAsync(cancellationToken);
            throw new McpTransportException("Failed to connect transport", ex);
        }
    }

    /// <inheritdoc/>
    public override async Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _process?.HasExited == true)
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

            // Write the message followed by a newline
            await _process!.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync(cancellationToken);

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

    private async Task ReadMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.TransportEnteringReadMessagesLoop(EndpointName);

            using var reader = _process!.StandardOutput;

            while (!cancellationToken.IsCancellationRequested && !_process.HasExited)
            {
                _logger.TransportWaitingForMessage(EndpointName);
                var line = await reader.ReadLineAsync(cancellationToken);
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
                        await WriteMessageAsync(message, cancellationToken);
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
            await CleanupAsync(cancellationToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        _logger.TransportCleaningUp(EndpointName);
        if (_process != null && _processStarted && !_process.HasExited)
        {
            try
            {
                // Try to close stdin to signal the process to exit
                _logger.TransportClosingStdin(EndpointName);
                _process.StandardInput.Close();

                // Wait for the process to exit
                _logger.TransportWaitingForShutdown(EndpointName);
                if (!_process.WaitForExit((int)_options.ShutdownTimeout.TotalMilliseconds))
                {
                    // If it doesn't exit gracefully, terminate it
                    _logger.TransportKillingProcess(EndpointName);
                    _process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                _logger.TransportShutdownFailed(EndpointName, ex);
            }

            _process.Dispose();
            _process = null;
        }

        if (_shutdownCts != null)
            await _shutdownCts.CancelAsync();
        _shutdownCts?.Dispose();
        _shutdownCts = null;

        if (_readTask != null)
        {
            try
            {
                _logger.TransportWaitingForReadTask(EndpointName);
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
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