namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Interface for providing an HTTP server to the SSE server transport implementation.
/// Library users need to implement this interface. A default implementation is provided by the library (TOOD).
/// </summary>
public interface IHttpServerProvider
{
    /// <summary>
    /// Gets the URI of the SSE endpoint.
    /// The transport implementation will return this URI to the client during the handshake.
    /// </summary>
    Task<string> GetSseEndpointUri();

    /// <summary>
    /// Initialize SSE event sending capability 
    /// </summary>
    Task InitializeSseEvents();

    /// <summary>
    /// Sends an event to the client.
    /// </summary>
    Task SendEvent(string data, string eventId);

    /// <summary>
    /// Initializes the message handler for POST requests.
    /// </summary>
    Task InitializeMessageHandler(Func<string, CancellationToken, Task<string>> messageHandler);

    /// <summary>
    /// Starts the HTTP server.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the HTTP server.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}