using System.Text.Json;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using McpDotNet.Utils.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Tests.Transport;

public class StdioServerTransportTests
{
    private readonly McpServerOptions _serverOptions;

    public StdioServerTransportTests()
    {
        _serverOptions = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = "Test Server",
                Version = "1.0"
            },
            ProtocolVersion = "2.0",
            InitializationTimeout = TimeSpan.FromSeconds(10),
            ServerInstructions = "Test Instructions"
        };
    }

    [Fact]
    public async Task Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Act
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(transport);
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new StdioServerTransport(serverOptions: null!, NullLoggerFactory.Instance));
        Assert.Equal("serverOptions", exception.ParamName);
    }

    [Fact]
    public async Task StartListeningAsync_Should_Set_Connected_State()
    {
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);

        await transport.StartListeningAsync();

        Assert.True(transport.IsConnected);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Send_Message()
    {
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);
        await transport.StartListeningAsync();

        var message = new JsonRpcRequest { Method = "test", Id = RequestId.FromNumber(44) };

        using var sw = new StringWriter();
        Console.SetOut(sw);

        await transport.SendMessageAsync(message);

        var result = sw.ToString().Trim();
        var expected = JsonSerializer.Serialize(message, JsonSerializerOptionsExtensions.DefaultOptions);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SendMessageAsync_Throws_Exception_If_Not_Connected()
    {
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);

        var message = new JsonRpcRequest { Method = "test" };

        await Assert.ThrowsAsync<McpTransportException>(() => transport.SendMessageAsync(message));
    }

    [Fact]
    public async Task DisposeAsync_Should_Dispose_Resources()
    {
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);

        await transport.DisposeAsync();

        Assert.False(transport.IsConnected);
    }

    [Fact]
    public async Task ReadMessagesAsync_Should_Read_Messages()
    {
        await using var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);
        await transport.StartListeningAsync();

        var message = new JsonRpcRequest { Method = "test", Id = RequestId.FromNumber(44) };
        var json = JsonSerializer.Serialize(message, JsonSerializerOptionsExtensions.DefaultOptions);

        using var sr = new StringReader(json);
        Console.SetIn(sr);

        var canRead = await transport.MessageReader.WaitToReadAsync();

        Assert.True(canRead, "Nothing to read here from transport message reader");
        Assert.True(transport.MessageReader.TryPeek(out var readMessage));
        Assert.NotNull(readMessage);
        Assert.IsType<JsonRpcRequest>(readMessage);
        Assert.Equal(44, ((JsonRpcRequest)readMessage).Id.AsNumber);
    }

    [Fact]
    public async Task CleanupAsync_Should_Cleanup_Resources()
    {
        var transport = new StdioServerTransport(_serverOptions, NullLoggerFactory.Instance);
        await transport.StartListeningAsync();

        await transport.DisposeAsync();

        Assert.False(transport.IsConnected);
    }
}
