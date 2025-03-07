using McpDotNet.Client;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using McpDotNet.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace McpDotNet.Tests.Server;

public class McpServerTests
{
    private readonly Mock<IServerTransport> _serverTransport;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<ILogger> _logger;
    private readonly McpServerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public McpServerTests()
    {
        _serverTransport = new Mock<IServerTransport>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _logger = new Mock<ILogger>();
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(_logger.Object);
        _options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "TestServer", Version = "1.0" },
            ProtocolVersion = "1.0",
            InitializationTimeout = TimeSpan.FromSeconds(30)
        };
        _serviceProvider = new Mock<IServiceProvider>().Object;
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange & Act
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Constructor_Throws_For_Null_Transport()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(null!, _options, _loggerFactory.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(_serverTransport.Object, null!, _loggerFactory.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_LoggerFactory()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServer(_serverTransport.Object, _options, null!, _serviceProvider));
    }

    [Fact]
    public void Constructor_Does_Not_Throw_For_Null_ServiceProvider()
    {
        // Arrange & Act
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, null);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Property_EndpointName_Return_Infos()
    {
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientInfo = new Implementation { Name = "TestClient", Version = "1.1" };
        Assert.Equal("Server (TestServer 1.0), Client (TestClient 1.1)", server.EndpointName);
    }

    [Fact]
    public async Task StartAsync_AlreadyInitializing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.GetType().GetField("_isInitializing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(server, true);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync());
    }

    [Fact]
    public async Task StartAsync_ShouldStartListening()
    {
        // Arrange
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);

        // Act
        await server.StartAsync();

        // Assert
        _serverTransport.Verify(t => t.StartListeningAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestSamplingAsync_ClientDoesNotSupportSampling_ShouldThrowMcpServerException()
    {
        // Arrange
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities();

        var action = () => server.RequestSamplingAsync(new CreateMessageRequestParams { Messages = [] }, CancellationToken.None);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<McpServerException>(action);
        Assert.Equal("Client does not support sampling", exception.Message);
    }

    [Fact]
    public async Task RequestSamplingAsync_ShouldSendRequest()
    {
        // Arrange
        var transport = new TestServerTransport();
        await using var server = new McpServer(transport, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Sampling = new SamplingCapability() };

        await server.StartAsync();

        // Act
        var result = await server.RequestSamplingAsync(new CreateMessageRequestParams { Messages = [] }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(transport.SentMessages);
        Assert.IsType<JsonRpcRequest>(transport.SentMessages[0]);
        Assert.Equal("sampling/createMessage", ((JsonRpcRequest)transport.SentMessages[0]).Method);
    }

    [Fact]
    public async Task RequestRootsAsync_ClientDoesNotSupportRoots_ShouldThrowMcpServerException()
    {
        // Arrange
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities();

        // Act & Assert
        await Assert.ThrowsAsync<McpServerException>(() => server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_Exception_If_Not_Connected()
    {
        var server = new McpServer(_serverTransport.Object, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Roots = new RootsCapability() };
        _serverTransport.SetupGet(t => t.IsConnected).Returns(false);

        var action = async () => await server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<McpClientException>(action);
        Assert.Equivalent("Transport is not connected", exception.Message);
    }

    [Fact]
    public async Task RequestRootsAsync_ShouldSendRequest()
    {
        // Arrange
        var transport = new TestServerTransport();
        await using var server = new McpServer(transport, _options, _loggerFactory.Object, _serviceProvider);
        server.ClientCapabilities = new ClientCapabilities { Roots = new RootsCapability() };
        await server.StartAsync();

        // Act
        var result = await server.RequestRootsAsync(new ListRootsRequestParams(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(transport.SentMessages);
        Assert.IsType<JsonRpcRequest>(transport.SentMessages[0]);
        Assert.Equal("roots/list", ((JsonRpcRequest)transport.SentMessages[0]).Method);
    }
}
