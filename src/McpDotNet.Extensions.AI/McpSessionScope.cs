using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Extensions.AI;

public class McpSessionScope : IAsyncDisposable
{
    private readonly List<IMcpClient> _clients = new();
    private bool _disposed;

    public IList<AITool> Tools { get; private set; }

    public IReadOnlyList<string> ServerInstructions { get; private set; } = [];

    public static async Task<McpSessionScope> CreateAsync(McpServerConfig serverConfig, 
        McpClientOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        var scope = new McpSessionScope();
        var client = await scope.AddClientAsync(serverConfig, options, loggerFactory);
        var tools = await client.ListToolsAsync();
        scope.Tools = tools.Tools.Select(t => new McpAIFunction(t, client)).ToList<AITool>();
        if (!string.IsNullOrEmpty(client.ServerInstructions))
        {
            scope.ServerInstructions = [client.ServerInstructions];
        }
        return scope;
    }

    public static async Task<McpSessionScope> CreateAsync(IEnumerable<McpServerConfig> serverConfigs, 
        McpClientOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        var scope = new McpSessionScope();
        foreach (var config in serverConfigs)
        {
            var client = await scope.AddClientAsync(config, options, loggerFactory);
            var tools = await client.ListToolsAsync();
            scope.Tools = scope.Tools.Concat(tools.Tools.Select(t => new McpAIFunction(t, client))).ToList();
        }
        scope.ServerInstructions = scope._clients.Select(c => c.ServerInstructions).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
        return scope;
    }

    private async Task<IMcpClient> AddClientAsync(McpServerConfig config,
        McpClientOptions? options,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = new McpClientFactory([config],
            options ?? new() { ClientInfo = new() { Name = "AnonymousClient", Version = "1.0.0.0" } },
            loggerFactory ?? NullLoggerFactory.Instance);
        var client = await factory.GetClientAsync(config.Id);
        _clients.Add(client);
        return client;
    }

    /// </inheritdoc>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var client in _clients)
        {
            await client.DisposeAsync();
        }
        _clients.Clear();
        Tools = [];
    }
}