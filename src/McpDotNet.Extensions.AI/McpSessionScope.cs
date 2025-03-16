using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Extensions.AI;

/// <summary>
/// Represents a scope that manages a connection session with one or more MCP servers.
/// Compatible with both stdio and SSE transports.
/// 
/// Disposing the scope will dispose all clients and tools, and close all connections.
/// </summary>
public sealed class McpSessionScope : IAsyncDisposable
{
    private readonly List<IMcpClient> _clients = [];
    private bool _disposed;

    /// <summary>
    /// The list of tools exposed by the servers in this scope.
    /// </summary>
    public IList<AITool> Tools { get; private set; } = [];

    /// <summary>
    /// The server instructions provided by the servers in this scope. 
    /// If a server does not provide instructions, they will not be included, so the list may be empty.
    /// </summary>
    public IReadOnlyList<string> ServerInstructions { get; private set; } = [];

    /// <summary>
    /// Creates a new session scope with a single server.
    /// </summary>
    /// <param name="serverConfig">A configuration object describing the MCP server.</param>
    /// <param name="options">An options object with the client name and capabilities. Passed to the server.</param>
    /// <param name="loggerFactory">A logger factory for mcpdotnet.</param>
    /// <returns>A session scope which will keep the connection alive until disposed.</returns>
    public static async Task<McpSessionScope> CreateAsync(McpServerConfig serverConfig,
        McpClientOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (serverConfig is null)
        {
            throw new ArgumentNullException(nameof(serverConfig));
        }

        var scope = new McpSessionScope();
        var client = await AddClientAsync(serverConfig, options, loggerFactory).ConfigureAwait(false);

        scope.Tools = [];
        await foreach (var tool in client.ListToolsAsync().ConfigureAwait(false))
        {
            scope.Tools.Add(new McpAIFunction(tool, client));
        }

        if (!string.IsNullOrEmpty(client.ServerInstructions))
        {
            scope.ServerInstructions = [client.ServerInstructions!];
        }

        return scope;
    }

    /// <summary>
    /// Creates a new session scope with multiple servers.
    /// </summary>
    /// <param name="serverConfigs">Configuration objects describing the MCP servers.</param>
    /// <param name="options">An options object with the client name and capabilities. Passed to the servers.</param>
    /// <param name="loggerFactory">A logger factory for mcpdotnet.</param>
    /// <returns>A session scope which keep the connections alive until disposed.</returns>
    public static async Task<McpSessionScope> CreateAsync(IEnumerable<McpServerConfig> serverConfigs,
        McpClientOptions? options = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (serverConfigs is null)
        {
            throw new ArgumentNullException(nameof(serverConfigs));
        }

        var scope = new McpSessionScope();

        foreach (var config in serverConfigs)
        {
            var client = await AddClientAsync(config, options, loggerFactory).ConfigureAwait(false);

            scope.Tools ??= [];
            await foreach (var tool in client.ListToolsAsync().ConfigureAwait(false))
            {
                scope.Tools.Add(new McpAIFunction(tool, client));
            }
        }

        scope.ServerInstructions = scope._clients.Select(c => c.ServerInstructions).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
        
        return scope;
    }

    private static Task<IMcpClient> AddClientAsync(
        McpServerConfig serverConfig,
        McpClientOptions? clientOptions,
        ILoggerFactory? loggerFactory = null)
    {
        return McpClientFactory.CreateAsync(
            serverConfig,
            clientOptions ?? new() { ClientInfo = new() { Name = "AnonymousClient", Version = "1.0.0.0" } }, 
            loggerFactory: loggerFactory);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var client in _clients)
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }
        _clients.Clear();
        Tools = [];

        GC.SuppressFinalize(this);
    }
}