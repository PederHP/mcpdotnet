using McpDotNet.Protocol.Transport;
using System.Collections.Concurrent;
using System.Net;
using System.Text;

public class HttpListenerServerProvider : IHttpServerProvider
{
    private readonly int _port;
    private readonly string _sseEndpoint = "/sse";
    private readonly string _messageEndpoint = "/message";
    private HttpListener _listener;
    private CancellationTokenSource _cts;
    private Func<string, CancellationToken, bool> _messageHandler;
    private ConcurrentDictionary<string, StreamWriter> _sseClients = new();
    private bool _isRunning;
    private readonly object _lock = new();

    public HttpListenerServerProvider(int port)
    {
        _port = port;
    }

    public Task<string> GetSseEndpointUri()
    {
        return Task.FromResult($"http://localhost:{_port}{_sseEndpoint}");
    }

    public Task InitializeMessageHandler(Func<string, CancellationToken, bool> messageHandler)
    {
        _messageHandler = messageHandler;
        return Task.CompletedTask;
    }

    public Task InitializeSseEvents()
    {
        return Task.CompletedTask;
    }

    public Task SendEvent(string data, string eventId)
    {
        foreach (var client in _sseClients)
        {
            try
            {
                if (eventId != null)
                {
                    client.Value.WriteLine($"id: {eventId}");
                }
                client.Value.WriteLine($"data: {data}");
                client.Value.WriteLine(); // Empty line to finish the event
                client.Value.Flush();
            }
            catch (Exception)
            {
                // Client disconnected, remove it
                _sseClients.TryRemove(client.Key, out _);
            }
        }
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
                return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            _isRunning = true;

            // Start listening for connections
            Task.Run(() => ListenForConnectionsAsync(_cts.Token));
            return Task.CompletedTask;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isRunning)
                return Task.CompletedTask;

            _cts?.Cancel();
            _listener?.Stop();

            foreach (var client in _sseClients)
            {
                try
                {
                    client.Value.Close();
                }
                catch { /* Ignore errors during shutdown */ }
            }
            _sseClients.Clear();

            _isRunning = false;
            return Task.CompletedTask;
        }
    }

    private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                // Process the request in a separate task
                _ = Task.Run(() => ProcessRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown requested, exit gracefully
                break;
            }
            catch (Exception)
            {
                // Log error but continue listening
                if (!cancellationToken.IsCancellationRequested)
                {
                    // Continue listening if not shutting down
                    continue;
                }
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // Handle SSE connection
            if (request.HttpMethod == "GET" && request.Url.LocalPath == _sseEndpoint)
            {
                await HandleSseConnectionAsync(context, cancellationToken);
            }
            // Handle message POST
            else if (request.HttpMethod == "POST" && request.Url.LocalPath == _messageEndpoint)
            {
                await HandleMessageAsync(context, cancellationToken);
            }
            else
            {
                // Not found
                response.StatusCode = 404;
                response.Close();
            }
        }
        catch (Exception)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { /* Ignore errors during error handling */ }
        }
    }

    private async Task HandleSseConnectionAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;

        // Set SSE headers
        response.ContentType = "text/event-stream";
        response.Headers.Add("Cache-Control", "no-cache");
        response.Headers.Add("Connection", "keep-alive");

        // Create a unique ID for this client
        var clientId = Guid.NewGuid().ToString();

        // Get the output stream and create a StreamWriter
        var outputStream = response.OutputStream;
        var writer = new StreamWriter(outputStream, Encoding.UTF8) { AutoFlush = true };

        // Add to active clients
        _sseClients.TryAdd(clientId, writer);

        // Keep the connection open until cancelled
        try
        {
            // Immediately send the "endpoint" event with the POST URL
            await writer.WriteLineAsync("event: endpoint");
            await writer.WriteLineAsync($"data: {_messageEndpoint}");
            await writer.WriteLineAsync(); // blank line to end an SSE message
            await writer.FlushAsync(cancellationToken);

            // Keep the connection open
            //await Task.Delay(-1, cancellationToken);
            // Keep the connection open by "pinging" or just waiting
            // until the client disconnects or the server is canceled.
            while (!cancellationToken.IsCancellationRequested && response.OutputStream.CanWrite)
            {
                // Optionally do a periodic no-op to keep connection alive:
                await writer.WriteLineAsync(": keep-alive");
                await writer.FlushAsync(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception)
        {
            // Client disconnected or other error
        }
        finally
        {
            // Remove client on disconnect
            _sseClients.TryRemove(clientId, out _);
            try
            {
                writer.Close();
                response.Close();
            }
            catch { /* Ignore errors during cleanup */ }
        }
    }

    private async Task HandleMessageAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        // Read the request body
        string requestBody;
        using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        // Process the message asynchronously
        if (_messageHandler(requestBody, cancellationToken))
        {
            // Return 202 Accepted
            response.StatusCode = 202;
            // Write "accepted" response
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Accepted"), cancellationToken);
        }
        else
        {
            // Return 400 Bad Request
            response.StatusCode = 400;
        }

        response.Close();
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _listener?.Close();
    }
}
