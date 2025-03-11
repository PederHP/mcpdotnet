using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Tests;

public class ClientIntegrationTestFixture : IDisposable
{
    public ILoggerFactory LoggerFactory { get; }
    public McpClientFactory Factory { get; }
    public McpClientOptions DefaultOptions { get; }

    public ClientIntegrationTestFixture()
    {
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        DefaultOptions = new()
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" },
            Capabilities = new() { Sampling = new(), Roots = new() }
        };

        var everythingServerConfig = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                // Change to ["arguments"] = "mcp-server-everything" if you want to run the server locally after creating a symlink
                ["arguments"] = "-y --verbose @modelcontextprotocol/server-everything"
            }
        };

        var testServerConfig = new McpServerConfig
        {
            Id = "test_server",
            Name = "TestServer",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = OperatingSystem.IsWindows() ? "TestServer.exe" : "dotnet",
                // Change to ["arguments"] = "mcp-server-everything" if you want to run the server locally after creating a symlink
            }
        };

        if (!OperatingSystem.IsWindows())
            testServerConfig.TransportOptions["arguments"] = "TestServer.dll";

        // Inject the mock transport into the factory
        Factory = new McpClientFactory(
            [everythingServerConfig, testServerConfig],
            DefaultOptions,
            LoggerFactory
        );
    }

    public void Dispose()
    {
        Factory?.Dispose();
        LoggerFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}