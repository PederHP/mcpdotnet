using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpDotNet.Tests.Client;

public class SseTransportTests
{
    [Fact]
    public void SseTransportConstructor_WithValidConfig_CreatesTransport()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080"
        };

        var transportOptions = new SseClientTransportOptions
        {
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            MaxReconnectAttempts = 2,
            ReconnectDelay = TimeSpan.FromSeconds(5),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["test"] = "header"
            }
        };

        // Act
        var transport = new SseClientTransport(transportOptions, config, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(transport);
        Assert.Equal(TimeSpan.FromSeconds(10), transport.Options.ConnectionTimeout);
        Assert.Equal(2, transport.Options.MaxReconnectAttempts);
        Assert.Equal(TimeSpan.FromSeconds(5), transport.Options.ReconnectDelay);
        Assert.NotNull(transport.Options.AdditionalHeaders);
        Assert.Equal("header", transport.Options.AdditionalHeaders["test"]);
    }

    [Fact]
    public async Task SseTransportSendMessageAsync_WithMessageEndpointNotSet_ThrowsException()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = TransportTypes.Sse,
            Location = "http://localhost:8080"
        };

        var transportOptions = new SseClientTransportOptions
        {
            ConnectionTimeout = TimeSpan.FromSeconds(10),
            MaxReconnectAttempts = 2,
            ReconnectDelay = TimeSpan.FromSeconds(5),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["test"] = "header"
            }
        };

        // Act
        var transport = new SseClientTransport(transportOptions, config, NullLoggerFactory.Instance);

        // Assert
        Assert.True(string.IsNullOrEmpty(transport.MessageEndpoint?.ToString()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendMessageAsync(new JsonRpcRequest() { Method = "test" }, CancellationToken.None));
    }
}