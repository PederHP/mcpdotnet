using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace McpDotNet.Tests.Server;

public class McpServerFactoryTests
{
    private readonly Mock<IServerTransport> _serverTransport;
    private readonly McpServerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public McpServerFactoryTests()
    {
        _serverTransport = new Mock<IServerTransport>();
        _options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "TestServer", Version = "1.0" },
            ProtocolVersion = "1.0",
            InitializationTimeout = TimeSpan.FromSeconds(30)
        };
        _serviceProvider = new Mock<IServiceProvider>().Object;
    }

    [Fact]
    public async Task Create_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange & Act
        await using IMcpServer server = McpServerFactory.Create(_serverTransport.Object, _options, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(server);
    }

    [Fact]
    public void Constructor_Throws_For_Null_ServerTransport()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>("serverTransport", () => McpServerFactory.Create(null!, _options, NullLoggerFactory.Instance));
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>("serverOptions", () => McpServerFactory.Create(_serverTransport.Object, null!, NullLoggerFactory.Instance));
    }
}
