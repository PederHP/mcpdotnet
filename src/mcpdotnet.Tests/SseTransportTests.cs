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
            TransportType = "sse",
            Location = "http://localhost:8080"
        };

        var transportOptions = new SseTransportOptions
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
        var transport = new SseTransport(transportOptions, config, NullLoggerFactory.Instance);

        // Assert
        Assert.NotNull(transport);
        Assert.True(transport.Options.ConnectionTimeout == TimeSpan.FromSeconds(10));
        Assert.True(transport.Options.MaxReconnectAttempts == 2);
        Assert.True(transport.Options.ReconnectDelay == TimeSpan.FromSeconds(5));
        Assert.True(transport.Options.AdditionalHeaders["test"] == "header");
    }

    [Fact]
    public async void SseTransportSendMessageAsync_WithMessageEndpointNotSet_ThrowsException()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "test-server",
            Name = "Test Server",
            TransportType = "sse",
            Location = "http://localhost:8080"
        };

        var transportOptions = new SseTransportOptions
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
        var transport = new SseTransport(transportOptions, config, NullLoggerFactory.Instance);

        // Assert
        Assert.True(string.IsNullOrEmpty(transport.MessageEndpoint?.ToString()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendMessageAsync(new JsonRpcRequest() { Method = "test"}, CancellationToken.None));
    }
}