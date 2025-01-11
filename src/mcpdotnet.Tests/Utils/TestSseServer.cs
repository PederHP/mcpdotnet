using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace mcpdotnet.Tests.Utils
{
    public sealed class TestSseServer : IAsyncDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts;
        private readonly ILogger<TestSseServer> _logger;
        private Task? _serverTask;
        private readonly string _endpointPath;
        private readonly string _messagePath;
        private readonly List<HttpListenerContext> _connections = new();

        public TestSseServer(int port = 5000, ILogger<TestSseServer>? logger = null)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _cts = new CancellationTokenSource();
            _logger = logger ?? NullLogger<TestSseServer>.Instance;

            // We'll use these paths for SSE connection and message posting
            _endpointPath = "/sse";
            _messagePath = "/message";
        }

        public string SseEndpoint => $"http://localhost:{_listener.Prefixes.First().Split(':')[2]}{_endpointPath}";

        public async Task StartAsync()
        {
            _listener.Start();
            _serverTask = HandleConnectionsAsync(_cts.Token);

            _logger.LogInformation("Test SSE server started on {Endpoint}", SseEndpoint);
        }

        private async Task HandleConnectionsAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var context = await _listener.GetContextAsync();
                    _ = HandleRequestAsync(context, ct);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error in SSE server connection handling");
            }
        }

        private object HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_serverTask != null)
                await _serverTask;

            _listener.Close();
            _cts.Dispose();

            _logger.LogInformation("Test SSE server stopped");
        }
    }
}
