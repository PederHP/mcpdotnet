// Protocol/Transport/StdioTransport.cs
namespace McpDotNet.Protocol.Transport;

using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using McpDotNet.Protocol.Messages;
using McpDotNet.Utils.Json;

/// <summary>
/// Represents configuration options for the stdio transport.
/// </summary>
public record StdioTransportOptions
{
    /// <summary>
    /// The command to execute to start the server process.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Arguments to pass to the server process.
    /// </summary>
    public string[]? Arguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The working directory for the server process.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Environment variables to set for the server process.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// The timeout to wait for the server to shut down gracefully.
    /// </summary>
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Implements the MCP transport protocol over standard input/output streams.
/// </summary>
public sealed class StdioTransport : TransportBase
{
    private readonly StdioTransportOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private Process? _process;
    private Task? _readTask;
    private CancellationTokenSource? _shutdownCts;
    private bool _processStarted;

    /// <summary>
    /// Initializes a new instance of the StdioTransport class.
    /// </summary>
    /// <param name="options">Configuration options for the transport.</param>
    public StdioTransport(StdioTransportOptions options)
    {
        _options = options;
        _jsonOptions = new JsonSerializerOptions().ConfigureForMcp();
    }

    /// <inheritdoc/>
    public override async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            throw new McpTransportException("Transport is already connected");
        }

        try
        {
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

            _process = new Process { StartInfo = startInfo };

            // Set up error logging
            _process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    Debug.WriteLine($"MCP Server Error: {args.Data}");
                }
            };

            if (!_process.Start())
            {
                Debug.WriteLine("Failed to start MCP server process");
                throw new McpTransportException("Failed to start MCP server process");
            }
            _processStarted = true;
            _process.BeginErrorReadLine();


            // Start reading messages in the background
            _readTask = Task.Run(async () => await ReadMessagesAsync(_shutdownCts.Token));

            SetConnected(true);
        }
        catch (Exception ex)
        {
            await CleanupAsync(cancellationToken);
            throw new McpTransportException("Failed to connect transport", ex);
        }
    }

    /// <inheritdoc/>
    public override async Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _process?.HasExited == true)
        {
            throw new McpTransportException("Transport is not connected");
        }

        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);

            // Write the message followed by a newline
            await _process!.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync();
        }
        catch (Exception ex)
        {
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
            using var reader = _process!.StandardOutput;

            while (!cancellationToken.IsCancellationRequested && !_process.HasExited)
            {
                Debug.WriteLine("Reading message...");
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                Debug.WriteLine($"Received stdio message: {line}");

                try
                {
                    var message = JsonSerializer.Deserialize<IJsonRpcMessage>(line, _jsonOptions);
                    if (message != null)
                    {

                        await WriteMessageAsync(message, cancellationToken);
                    }
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"Failed to parse message: {ex.Message}");
                    // Continue reading even if we fail to parse a message
                }
            }
            Debug.WriteLine("Exiting read loop");
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading messages: {ex}");
        }
        finally
        {
            Debug.WriteLine("Entering ReadMessages cleanup");
            await CleanupAsync(cancellationToken);
            Debug.WriteLine("Completed ReadMessages cleanup");
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (_process != null && _processStarted && !_process.HasExited)
        {
            
            try
            {
                // Try to close stdin to signal the process to exit
                _process.StandardInput.Close();

                // Wait for the process to exit
                if (!_process.WaitForExit((int)_options.ShutdownTimeout.TotalMilliseconds))
                {
                    // If it doesn't exit gracefully, terminate it
                    _process.Kill(true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during cleanup: {ex}");
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
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                Debug.WriteLine("Cleanup timeout waiting for read task");
                // Continue with cleanup
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error waiting for read task: {ex}");
            }
            _readTask = null;
        }

        SetConnected(false);
    }

}