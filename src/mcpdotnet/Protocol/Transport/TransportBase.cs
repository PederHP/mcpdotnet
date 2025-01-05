using System.Threading.Channels;
using McpDotNet.Protocol.Messages;

namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Base class for implementing MCP transports with common functionality.
/// </summary>
public abstract class TransportBase : IMcpTransport
{
    private readonly Channel<IJsonRpcMessage> _messageChannel;
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransportBase"/> class.
    /// </summary>
    protected TransportBase()
    {
        // Unbounded channel to prevent blocking on writes
        _messageChannel = Channel.CreateUnbounded<IJsonRpcMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        });
    }

    /// <inheritdoc/>
    public bool IsConnected => _isConnected;

    /// <inheritdoc/>
    public ChannelReader<IJsonRpcMessage> MessageReader => _messageChannel.Reader;

    /// <inheritdoc/>
    public abstract Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract ValueTask DisposeAsync();

    /// <summary>
    /// Writes a message to the message channel.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    protected async Task WriteMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            throw new McpTransportException("Transport is not connected");
        }

        await _messageChannel.Writer.WriteAsync(message, cancellationToken);
    }

    /// <summary>
    /// Sets the connected state of the transport.
    /// </summary>
    /// <param name="isConnected">Whether the transport is connected.</param>
    protected void SetConnected(bool isConnected)
    {
        if (_isConnected == isConnected)
        {
            return;
        }

        _isConnected = isConnected;
        if (!isConnected)
        {
            _messageChannel.Writer.Complete();
        }
    }
}