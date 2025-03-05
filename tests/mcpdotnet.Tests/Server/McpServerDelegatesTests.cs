using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Moq;

namespace mcpdotnet.Tests.Server;

public class McpServerDelegatesTests
{
    [Fact]
    public void Applies_All_Given_Delegates()
    {
        var container = new McpServerDelegates();
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        container.ListToolsHandler = (p, c) => Task.FromResult(new ListToolsResult());
        container.CallToolHandler = (p, c) => Task.FromResult(new CallToolResponse());
        container.ListPromptsHandler = (p, c) => Task.FromResult(new ListPromptsResult());
        container.GetPromptHandler = (p, c) => Task.FromResult(new GetPromptResult());
        container.ListResourcesHandler = (p, c) => Task.FromResult(new ListResourcesResult());
        container.ReadResourceHandler = (p, c) => Task.FromResult(new ReadResourceResult());
        container.GetCompletionHandler = (p, c) => Task.FromResult(new CompleteResult());
        container.SubscribeToResourcesHandler = (s, c) => Task.CompletedTask;
        container.UnsubscribeFromResourcesHandler = (s, c) => Task.CompletedTask;

        container.Apply(server.Object);

        Assert.Equal(container.ListToolsHandler, server.Object.ListToolsHandler);
        Assert.Equal(container.CallToolHandler, server.Object.CallToolHandler);
        Assert.Equal(container.ListPromptsHandler, server.Object.ListPromptsHandler);
        Assert.Equal(container.GetPromptHandler, server.Object.GetPromptHandler);
        Assert.Equal(container.ListResourcesHandler, server.Object.ListResourcesHandler);
        Assert.Equal(container.ReadResourceHandler, server.Object.ReadResourceHandler);
        Assert.Equal(container.GetCompletionHandler, server.Object.GetCompletionHandler);
        Assert.Equal(container.SubscribeToResourcesHandler, server.Object.SubscribeToResourcesHandler);
        Assert.Equal(container.UnsubscribeFromResourcesHandler, server.Object.UnsubscribeFromResourcesHandler);
    }
}
