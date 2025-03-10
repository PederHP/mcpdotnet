using System.Net;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Tests.Client;

public class SseClientTransportTests
{
    private readonly McpServerConfig _serverConfig;
    private readonly SseClientTransportOptions _transportOptions;

    public SseClientTransportTests()
    {
        _serverConfig = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080"
        };

        _transportOptions = new SseClientTransportOptions
        {
            ConnectionTimeout = TimeSpan.FromSeconds(2),
            MaxReconnectAttempts = 3,
            ReconnectDelay = TimeSpan.FromMilliseconds(50),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["test"] = "header"
            }
        };
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Act
        var transport = new SseClientTransport(_transportOptions, _serverConfig, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(transport);
        Assert.Equal(TimeSpan.FromSeconds(2), transport.Options.ConnectionTimeout);
        Assert.Equal(3, transport.Options.MaxReconnectAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(50), transport.Options.ReconnectDelay);
        Assert.NotNull(transport.Options.AdditionalHeaders);
        Assert.Equal("header", transport.Options.AdditionalHeaders["test"]);
    }

    [Fact]
    public void Constructor_Throws_For_Null_Options()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SseClientTransport(null!, _serverConfig, NullLoggerFactory.Instance));
        Assert.Equal("transportOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_Throws_For_Null_Config()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SseClientTransport(_transportOptions, null!, NullLoggerFactory.Instance));
        Assert.Equal("serverConfig", exception.ParamName);
    }

    [Fact]
    public void Constructor_Throws_For_Null_HttpClientg()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new SseClientTransport(_transportOptions, _serverConfig, null!, NullLoggerFactory.Instance));
        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public async Task ConnectAsync_Should_Connect_Successfully()
    {
        var mockHttpHandler = new MockHttpHandler();
        var httpClient = new HttpClient(mockHttpHandler);
        await using var transport = new SseClientTransport(_transportOptions, _serverConfig, httpClient, NullLoggerFactory.Instance);

        bool firstCall = true;

        mockHttpHandler.RequestHandler = async (request) =>
        {
            if (!firstCall)
            {
                Assert.True(transport.IsConnected);
                await transport.CloseAsync();
            }

            firstCall = false;
            return new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("event: endpoint\r\ndata: http://localhost\r\n\r\n")
            };
        };

        await transport.ConnectAsync();
    }

    [Fact]
    public async Task ConnectAsync_Throws_Exception_On_Failure()
    {
        var mockHttpHandler = new MockHttpHandler();
        var httpClient = new HttpClient(mockHttpHandler);
        await using var transport = new SseClientTransport(_transportOptions, _serverConfig, httpClient, NullLoggerFactory.Instance);

        var retries = 0;
        mockHttpHandler.RequestHandler = (request) =>
        {
            retries++;
            throw new InvalidOperationException("Test exception");
        };

        var action = async () => await transport.ConnectAsync();

        var exception = await Assert.ThrowsAsync<McpTransportException>(action);
        Assert.Equal("Exceeded reconnect limit", exception.Message);

        Assert.Equal(_transportOptions.MaxReconnectAttempts, retries);
    }

    [Fact]
    public async Task SendMessageAsync_Throws_Exception_If_MessageEndpoint_Not_Set()
    {
        await using var transport = new SseClientTransport(_transportOptions, _serverConfig, NullLoggerFactory.Instance);

        // Assert
        Assert.True(string.IsNullOrEmpty(transport.MessageEndpoint?.ToString()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendMessageAsync(new JsonRpcRequest() { Method = "test" }, CancellationToken.None));
    }

    //[Fact]
    //public async Task SendMessageAsync_Sends_Message()
    //{
    //    var httpClient = new HttpClient(_mockHttpHandler);
    //    await using var transport = new SseClientTransport(_transportOptions, _serverConfig, httpClient, NullLoggerFactory.Instance);

    //    _mockHttpHandler.RequestHandler = async (request) =>
    //    {

    //        return new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent("event: endpoint\r\ndata: /sseendpoint\r\n\r\n")
    //        };
    //    };

    //    await transport.ConnectAsync();

    //    await transport.SendMessageAsync(new JsonRpcRequest() { Method = "test" }, CancellationToken.None);
    //}

    [Fact]
    public async Task CloseAsync_Should_Dispose_Resources()
    {
        var transport = new SseClientTransport(_transportOptions, _serverConfig, NullLoggerFactory.Instance);

        await transport.CloseAsync();

        Assert.False(transport.IsConnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_Dispose_Resources()
    {
        var transport = new SseClientTransport(_transportOptions, _serverConfig, NullLoggerFactory.Instance);

        await transport.DisposeAsync();

        Assert.False(transport.IsConnected);
    }
}