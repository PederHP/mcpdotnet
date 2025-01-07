using Castle.Core.Logging;
using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading.Channels;
using Xunit;

namespace McpDotNet.Tests.Client;

public class McpClientFactoryTests
{
    private readonly McpClientOptions _defaultOptions = new()
    {
        ClientInfo = new() { Name = "TestClient", Version = "1.0.0" }
    };

    [Fact]
    public async Task GetClientAsync_WithValidStdioConfig_CreatesNewClient()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "stdio",
            Location = "/path/to/server",
            TransportOptions = new Dictionary<string, string>
            {
                ["arguments"] = "--test arg",
                ["workingDirectory"] = "/working/dir"
            }
        };

        // Create a mock transport
        var mockTransport = new Mock<IMcpTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        // Inject the mock transport into the factory
        var factory = new McpClientFactory(
            [config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object,
            clientFactoryMethod: (_, _, _) => mockClient.Object
        );

        // Act
        var client = await factory.GetClientAsync("test-server");

        // Assert
        Assert.NotNull(client);
        // We could add more assertions here about the client's configuration
    }

    [Fact]
    public async Task GetClientAsync_CalledTwice_ReturnsSameInstance()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "stdio",
            Location = "/path/to/server"
        };

        // Create a mock transport
        var mockTransport = new Mock<IMcpTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        var factory = new McpClientFactory([config], 
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object, 
            clientFactoryMethod: (_,_,_) => mockClient.Object);

        // Act
        var client1 = await factory.GetClientAsync("test-server");
        var client2 = await factory.GetClientAsync("test-server");

        // Assert
        Assert.Same(client1, client2);
    }

    [Fact]
    public async Task GetClientAsync_WithInvalidServerId_ThrowsArgumentException()
    {
        // Arrange
        var factory = new McpClientFactory(Array.Empty<McpServerConfig>(), 
            _defaultOptions,
            NullLoggerFactory.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.GetClientAsync("non-existent-server")
        );
    }

    [Fact]
    public async Task GetClientAsync_WithUnsupportedTransport_ThrowsArgumentException()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "unsupported",
            Location = "/path/to/server"
        };

        var factory = new McpClientFactory([config], _defaultOptions,
            NullLoggerFactory.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.GetClientAsync("test-server")
        );
    }

    [Fact]
    public async Task GetClientAsync_WithNoTransportOptions_CreatesClientWithDefaults()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "stdio",
            Location = "/path/to/server"
        };

        // Create a mock transport
        var mockTransport = new Mock<IMcpTransport>();
        mockTransport.Setup(t => t.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockTransport.Setup(t => t.IsConnected).Returns(true);
        mockTransport.Setup(t => t.MessageReader).Returns(Mock.Of<ChannelReader<IJsonRpcMessage>>());

        // Create a mock client
        var mockClient = new Mock<IMcpClient>();
        mockClient.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockClient.Setup(c => c.IsInitialized).Returns(true);

        var factory = new McpClientFactory([config],
            _defaultOptions,
            NullLoggerFactory.Instance,
            transportFactoryMethod: _ => mockTransport.Object, 
            clientFactoryMethod: (_, _, _) => mockClient.Object);

        // Act
        var client = await factory.GetClientAsync("test-server");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithDuplicateServerIds_ThrowsArgumentException()
    {
        // Arrange
        McpServerConfig[] configs =
        [
            new McpServerConfig { Id = "duplicate", Name = "duplicate", TransportType = "stdio", Location = "/path1" },
            new McpServerConfig { Id = "duplicate", Name = "duplicate", TransportType = "stdio", Location = "/path2" }
        ];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new McpClientFactory(configs, _defaultOptions,
            NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task GetClientAsync_WithHttpTransport_ThrowsNotImplementedException()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "http",
            Location = "http://localhost:8080"
        };

        var factory = new McpClientFactory([config], _defaultOptions,
            NullLoggerFactory.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => factory.GetClientAsync("test-server")
        );
    }
}