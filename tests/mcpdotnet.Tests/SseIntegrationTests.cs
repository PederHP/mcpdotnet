using System.Text.Json;
using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Tests.Utils;
using Microsoft.Extensions.Logging;

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
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:5000/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        // Send a test message through POST endpoint
        await client.SendNotificationAsync("test/message", new { message = "Hello, SSE!" });

        // Assert
        Assert.True(true);
    }

    [Fact]
    public async Task ConnectAndReceiveMessage_EverythingServerWithSse()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        await using var fixture = new EverythingSseServerFixture();
        await fixture.StartAsync();

        var defaultOptions = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var defaultConfig = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:3001/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Create client and run tests
        var client = await factory.GetClientAsync("everything");
        var tools = await client.ListToolsAsync();

        // assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools.Tools);
    }

    [Fact]
    public async Task Sampling_Sse_EverythingServer()
    {
        // arrange
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        await using var fixture = new EverythingSseServerFixture();
        await fixture.StartAsync();

        var defaultOptions = new McpClientOptions
        {
            ClientInfo = new()
            {
                Name = "IntegrationTestClient",
                Version = "1.0.0"
            },
            Capabilities = new()
            {
                Sampling = new()
            }
        };

        var defaultConfig = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:3001/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );
        var client = await factory.GetClientAsync("everything");

        // Set up the sampling handler
        int samplingHandlerCalls = 0;
        client.SamplingHandler = (_, _) =>
        {
            samplingHandlerCalls++;
            return Task.FromResult(new CreateMessageResult
            {
                Model = "test-model",
                Role = "assistant",
                Content = new Content
                {
                    Type = "text",
                    Text = "Test response"
                }
            });
        };

        // Call the server's sampleLLM tool which should trigger our sampling handler
        var result = await client.CallToolAsync(
            "sampleLLM",
            new Dictionary<string, object>
            {
                ["prompt"] = "Test prompt",
                ["maxTokens"] = 100
            }
        );

        // assert
        Assert.NotNull(result);
        var textContent = Assert.Single(result.Content);
        Assert.Equal("text", textContent.Type);
        Assert.False(string.IsNullOrEmpty(textContent.Text));
    }

    [Fact]
    public async Task ConnectAndReceiveMessage_InMemoryServer_WithFullEndpointEventUri()
    {
        // Arrange
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            builder.AddConsole()
            .SetMinimumLevel(LogLevel.Debug));

        await using TestSseServer server = new(logger: loggerFactory.CreateLogger<TestSseServer>());
        server.UseFullUrlForEndpointEvent = true;
        await server.StartAsync();


        var defaultOptions = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var defaultConfig = new McpServerConfig
        {
            Id = "test_server",
            Name = "In-memory Test Server",
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:5000/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        // Send a test message through POST endpoint
        await client.SendNotificationAsync("test/message", new { message = "Hello, SSE!" });

        // Assert
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
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:5000/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        var receivedNotification = new TaskCompletionSource<string?>();
        client.OnNotification("test/notification", (args) =>
            {
                var msg = ((JsonElement?)args.Params)?.GetProperty("message").GetString();
                receivedNotification.SetResult(msg);

                return Task.CompletedTask;
            });

        // Act
        await server.SendTestNotificationAsync("Hello from server!");

        // Assert
        var message = await receivedNotification.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("Hello from server!", message);
    }

    [Fact]
    public async Task ConnectTwice_Throws()
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
            TransportType = TransportTypes.Sse,
            TransportOptions = [],
            Location = "http://localhost:5000/sse"
        };

        using var factory = new McpClientFactory(
            [defaultConfig],
            defaultOptions,
            loggerFactory
        );

        // Act
        var client = await factory.GetClientAsync("test_server");
        var mcpClient = (McpClient)client;
        var transport = (SseClientTransport)mcpClient.Transport;

        // Wait for SSE connection to be established
        await server.WaitForConnectionAsync(TimeSpan.FromSeconds(10));

        // Assert
        await Assert.ThrowsAsync<McpTransportException>(async () => await transport.ConnectAsync());
    }
}
