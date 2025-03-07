using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace McpDotNet.Tests.Server;

public class McpServerFactoryTests
{
    private readonly Mock<IServerTransport> _serverTransport;
    private readonly Mock<ILoggerFactory> _loggerFactory;
    private readonly Mock<IOptions<McpServerDelegates>> _serverDelegates;
    private readonly McpServerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public McpServerFactoryTests()
    {
        _serverTransport = new Mock<IServerTransport>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _serverDelegates = new Mock<IOptions<McpServerDelegates>>();
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
        var factory = new McpServerFactory(_serverTransport.Object, _options, _loggerFactory.Object, _serverDelegates.Object, _serviceProvider);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_Throws_For_Null_ServerTransport()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServerFactory(null!, _options, _loggerFactory.Object, _serverDelegates.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServerFactory(_serverTransport.Object, null!, _loggerFactory.Object, _serverDelegates.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Throws_For_Null_Logger()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new McpServerFactory(_serverTransport.Object, _options, null!, _serverDelegates.Object, _serviceProvider));
    }

    [Fact]
    public void Constructor_Does_Not_Throw_For_Null_ServerDelegates()
    {
        var factory = new McpServerFactory(_serverTransport.Object, _options, _loggerFactory.Object, null, _serviceProvider);
        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_Does_Not_Throw_For_Null_ServiceProvider()
    {
        var factory = new McpServerFactory(_serverTransport.Object, _options, _loggerFactory.Object, _serverDelegates.Object, null);
        Assert.NotNull(factory);
    }

    [Fact]
    public void CreateServer_Return_IMcpServerInstance()
    {
        // Arrange
        var factory = new McpServerFactory(_serverTransport.Object, _options, _loggerFactory.Object, _serverDelegates.Object, _serviceProvider);

        // Act
        var server = factory.CreateServer();

        // Assert
        Assert.NotNull(server);
        Assert.IsAssignableFrom<IMcpServer>(server);
    }
}
