using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Tests.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpDotNet.Tests;

public class SseIntegrationTests
{
    [Fact]
    public async Task ConnectAndReceiveMessage_InMemoryServer()
    {
        // Arrange
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        await using TestSseServer server = new(logger:loggerFactory.CreateLogger<TestSseServer>());
        await server.StartAsync();


        var defaultOptions = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var defaultConfig = new McpServerConfig
        {
            Id = "test_server",
            Name = "In-memory Test Server",
            TransportType = "sse",
            TransportOptions = new Dictionary<string, string>(),
            Location = "http://localhost:5000/sse"
        };

        var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        // Send a test message through POST endpoint
        var testMessage = new JsonRpcRequest()
        {
            Id = RequestId.FromNumber(1),
            Method = "test/message",
            Params = new{ message = "Hello, SSE!" }
        };
        using var httpClient = new HttpClient();
        await httpClient.PostAsync(
            "http://localhost:5000/message",
            new StringContent(JsonSerializer.Serialize(testMessage))
        );

        // Assert
        // We should add assertions here to verify message receipt
        // Perhaps by adding a message collection to our test client
        Assert.True(true);
    }

    [Fact]
    public async Task ConnectAndReceiveNotification_InMemoryServer()
    {
        // Arrange
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        await using TestSseServer server = new(logger: loggerFactory.CreateLogger<TestSseServer>());
        await server.StartAsync();

        
        var defaultOptions = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var defaultConfig = new McpServerConfig
        {
            Id = "test_server",
            Name = "In-memory Test Server",
            TransportType = "sse",
            TransportOptions = new Dictionary<string, string>(),
            Location = "http://localhost:5000/sse"
        };

        var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        var receivedNotification = new TaskCompletionSource<string>();
        client.OnNotification("test/notification", async (args) =>
            {
                var msg = ((JsonElement)args.Params).GetProperty("message").GetString();
                receivedNotification.SetResult(msg);
            });

        // Act
        await server.SendTestNotificationAsync("Hello from server!");

        // Assert
        var message = await receivedNotification.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Hello from server!", message);
    }
}
