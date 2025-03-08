using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace McpDotNet.Tests;

public class McpServerBuilderExtensionsTransportsTests
{
    [Fact]
    public void WithStdioServerTransport_Sets_Transport()
    {
        var services = new ServiceCollection();
        var builder = new Mock<IMcpServerBuilder>();
        builder.SetupGet(b => b.Services).Returns(services);

        builder.Object.WithStdioServerTransport();

        var transportType = services.FirstOrDefault(s => s.ServiceType == typeof(IServerTransport));
        Assert.NotNull(transportType);
        Assert.Equal(typeof(StdioServerTransport), transportType.ImplementationType);
    }
}
