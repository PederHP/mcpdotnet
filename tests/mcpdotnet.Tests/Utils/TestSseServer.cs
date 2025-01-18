using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using McpDotNet.Protocol.Messages;

namespace McpDotNet.Tests.Utils;

public sealed class TestSseServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly ILogger<TestSseServer> _logger;
    private Task? _serverTask;
    private readonly TaskCompletionSource _connectionEstablished = new();

    // SSE endpoint for GET
    private readonly string _endpointPath;
    // POST endpoint
    private readonly string _messagePath;

    // Keep track of all open SSE connections (StreamWriters).
    private readonly ConcurrentBag<StreamWriter> _sseClients = new();

    public TestSseServer(int port = 5000, ILogger<TestSseServer>? logger = null)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _cts = new CancellationTokenSource();
        _logger = logger ?? NullLogger<TestSseServer>.Instance;

        _endpointPath = "/sse";
        _messagePath = "/message";
    }

    /// <summary>
    /// Full URL for the SSE endpoint, e.g. "http://localhost:5000/sse"
    /// </summary>
    public string SseEndpoint
        => $"http://localhost:{_listener.Prefixes.First().Split(':')[2].TrimEnd('/')}{_endpointPath}";

    /// <summary>
    /// Full URL for the message endpoint, e.g. "http://localhost:5000/message"
    /// </summary>
    public string MessageEndpoint
        => $"http://localhost:{_listener.Prefixes.First().Split(':')[2].TrimEnd('/')}{_messagePath}";

    /// <summary>
    /// Starts the server so it can accept incoming connections and POST requests.
    /// </summary>
    public async Task StartAsync()
    {
        _listener.Start();
        _serverTask = HandleConnectionsAsync(_cts.Token);

        _logger.LogInformation("Test SSE server started on {Endpoint}", SseEndpoint);
        await Task.CompletedTask;
    }

    private async Task HandleConnectionsAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, ct), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore, we are shutting down
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Error in SSE server connection handling");
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        // Handle SSE endpoint
        if (request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
            && request.Url.AbsolutePath.Equals(_endpointPath, StringComparison.OrdinalIgnoreCase))
        {
            await HandleSseConnectionAsync(context, ct);
        }
        // Handle POST /message
        else if (request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
                 && request.Url.AbsolutePath.Equals(_messagePath, StringComparison.OrdinalIgnoreCase))
        {
            await HandlePostMessageAsync(context, ct);
        }
        else
        {
            response.StatusCode = 404;
            response.Close();
        }
    }

    /// <summary>
    /// Handle Server-Sent Events (SSE) connection.
    /// Send the initial event: endpoint with the full POST URL.
    /// Keep the connection open until the server is disposed or the client disconnects.
    /// </summary>
    private async Task HandleSseConnectionAsync(HttpListenerContext context, CancellationToken ct)
    {
        var response = context.Response;
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        // Ensures the response is never chunked away by the framework
        response.SendChunked = true;
        response.StatusCode = (int)HttpStatusCode.OK;

        using var writer = new StreamWriter(response.OutputStream);
        _sseClients.Add(writer);

        // Immediately send the "endpoint" event with the POST URL
        var endpointData = new
        {
            uri = MessageEndpoint
        };
        var json = JsonSerializer.Serialize(endpointData);

        await writer.WriteLineAsync("event: endpoint");
        await writer.WriteLineAsync($"data: {json}");
        await writer.WriteLineAsync(); // blank line to end an SSE message
        await writer.FlushAsync();

        _logger.LogInformation("New SSE client connected.");
        _connectionEstablished.TrySetResult(); // Signal connection is ready

        try
        {
            // Keep the connection open by "pinging" or just waiting
            // until the client disconnects or the server is canceled.
            while (!ct.IsCancellationRequested && response.OutputStream.CanWrite)
            {
                _logger.LogDebug("SSE connection alive check");
                // Optionally do a periodic no-op to keep connection alive:
                await writer.WriteLineAsync(": keep-alive");
                await writer.FlushAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
        catch (TaskCanceledException)
        {
            // This is expected on shutdown
            _logger.LogInformation("SSE connection cancelled (expected on shutdown)");
        }
        catch (IOException)
        {
            // Client likely disconnected
            _logger.LogInformation("SSE client disconnected");
        }
        finally
        {
            // Remove this writer from bag (we're disposing it anyway)
            _sseClients.TryTake(out _);
            _logger.LogInformation("SSE client disconnected.");
        }
    }

    // Add method to wait for connection
    public Task WaitForConnectionAsync(TimeSpan timeout) =>
        _connectionEstablished.Task.WaitAsync(timeout);

    /// <summary>
    /// Handle POST /message endpoint.
    /// Echo the content back to the caller and broadcast it over SSE as well.
    /// </summary>
    private async Task HandlePostMessageAsync(HttpListenerContext context, CancellationToken ct)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            using var reader = new StreamReader(request.InputStream);
            string content = await reader.ReadToEndAsync();

            var jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(content);

            if (jsonRpcRequest != null && !jsonRpcRequest.Method.StartsWith("notification"))
            {
                if (jsonRpcRequest.Method == "initialize")
                {
                    await HandleInitializationRequest(response, jsonRpcRequest);
                    return;
                }

                // Method not found - proper JSON-RPC error response
                await SendJsonRpcErrorAsync(response, jsonRpcRequest.Id, -32601, $"Method '{jsonRpcRequest.Method}' not found");
                return;
            }
 
            var notification = JsonSerializer.Deserialize<JsonRpcNotification>(content);
            if (notification != null)
            {
                response.StatusCode = 200;
                response.ContentType = "application/json";
                await using var writer = new StreamWriter(response.OutputStream);
                await writer.WriteAsync(JsonSerializer.Serialize(new { jsonrpc = "2.0" }));
                return;
            }

            // Invalid JSON-RPC message - proper JSON-RPC error response
            await SendJsonRpcErrorAsync(response, null, -32700, "Parse error");
        }
        catch (Exception ex)
        {
            await SendJsonRpcErrorAsync(response, null, -32603, "Internal error", ex.Message);
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    private async Task SendJsonRpcErrorAsync(HttpListenerResponse response, RequestId? id, int code, string message, string? data = null)
    {
        var errorResponse = new JsonRpcError
        {
            Id = id ?? RequestId.FromString("error"),
            JsonRpc = "2.0",
            Error = new JsonRpcErrorDetail
            {
                Code = code,
                Message = message,
                Data = data
            }
        };

        response.StatusCode = 200; // Always 200 for JSON-RPC
        response.ContentType = "application/json";
        await using var writer = new StreamWriter(response.OutputStream);
        await writer.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }

    private async Task HandleInitializationRequest(HttpListenerResponse response, JsonRpcRequest jsonRpcRequest)
    {
        // We don't need to validate the client's initialization request for the test
        // Just send back a valid server initialization response
        var jsonResponse = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                experimental = (object?)null,
                roots = (object?)null,
                sampling = (object?)null
            }
        };

        var jsonRpcResponse = new JsonRpcResponse()
        {
            Id = jsonRpcRequest.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    experimental = (object?)null,
                    roots = (object?)null,
                    sampling = (object?)null
                },
                serverInfo = new
                {
                    name = "ExampleServer",
                    version = "1.0.0"
                }
            }
        };

        // Echo back to the HTTP response
        response.StatusCode = 200;
        response.ContentType = "application/json";

        await using var writer = new StreamWriter(response.OutputStream);
        await writer.WriteAsync(JsonSerializer.Serialize(jsonRpcResponse));
    }

    /// <summary>
    /// Broadcast a message to all currently connected SSE clients.
    /// </summary>
    /// <param name="message">The raw string to send</param>
    public async Task BroadcastMessageAsync(string message)
    {
        foreach (var client in _sseClients.ToArray()) // ToArray to avoid mutation issues
        {
            try
            {
                // SSE requires "event: <name>" + "data: <payload>" + blank line
                await client.WriteLineAsync("event: message");
                await client.WriteLineAsync($"data: {message}");
                await client.WriteLineAsync();
                await client.FlushAsync();
            }
            catch (IOException)
            {
                // Client may have disconnected. We let them get cleaned up on next iteration.
            }
            catch (ObjectDisposedException)
            {
                // Stream is disposed, ignore.
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
        }

        _listener.Close();
        _cts.Dispose();

        _logger.LogInformation("Test SSE server stopped");
    }

    /// <summary>
    /// Send a test notification to all connected SSE clients.
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public async Task SendTestNotificationAsync(string content)
    {
        var notification = new JsonRpcNotification
        {
            JsonRpc = "2.0",
            Method = "test/notification",
            Params = new { message = content }
        };

        var serialized = JsonSerializer.Serialize(notification);
        await BroadcastMessageAsync(serialized);
    }
}
