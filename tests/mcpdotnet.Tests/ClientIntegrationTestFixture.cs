using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Tests;

public class ClientIntegrationTestFixture : IDisposable
{
    public ILoggerFactory LoggerFactory { get; }
    public McpClientFactory Factory { get; }
    public McpClientOptions DefaultOptions { get; }
    public McpServerConfig DefaultConfig { get; }

    public ClientIntegrationTestFixture()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        DefaultOptions = new()
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        DefaultConfig = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                // Change to ["arguments"] = "mcp-server-everything" if you want to run the server locally after creating a symlink
                ["arguments"] = "-y @modelcontextprotocol/server-everything",
            }
        };

        // Inject the mock transport into the factory
        Factory = new McpClientFactory(
            [DefaultConfig],
            DefaultOptions,
            LoggerFactory
        );
    }

    public void Dispose()
    {
        LoggerFactory?.Dispose();
    }
}