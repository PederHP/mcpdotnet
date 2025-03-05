using McpDotNet.Server;
using Microsoft.Extensions.Hosting;

namespace McpDotNet.Hosting;

/// <summary>
/// Hosted service for the MCP server.
/// </summary>
public class McpServerHostedService : BackgroundService
{
    private readonly IMcpServer _server;

    /// <summary>
    /// Creates a new instance of the McpServerHostedService.
    /// </summary>
    /// <param name="server">The MCP server instance</param>
    /// <exception cref="ArgumentNullException"></exception>
    public McpServerHostedService(IMcpServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _server.StartAsync(stoppingToken);
    }
}
