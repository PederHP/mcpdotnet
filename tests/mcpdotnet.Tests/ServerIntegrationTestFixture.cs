using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace McpDotNet.Tests;

public class ServerIntegrationTestFixture : IDisposable
{
    public ILoggerFactory LoggerFactory { get; }
    public McpClientFactory Factory { get; }
    public McpClientOptions DefaultOptions { get; }
    public McpServerConfig DefaultConfig { get; }

    public ServerIntegrationTestFixture()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        DefaultOptions = new()
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" },
            Capabilities = new() { Sampling = new(), Roots = new() }
        };

        DefaultConfig = new McpServerConfig
        {
            Id = "test_server",
            Name = "TestServer",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "TestServer.exe",
                // Change to ["arguments"] = "mcp-server-everything" if you want to run the server locally after creating a symlink
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
        var client = Factory.GetClientAsync("test_server").Result;
        client.DisposeAsync().AsTask().Wait();
        LoggerFactory?.Dispose();
    }
}