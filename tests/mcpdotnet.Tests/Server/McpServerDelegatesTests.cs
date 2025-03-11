using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;

namespace McpDotNet.Tests.Server;

public class McpServerDelegatesTests
{
    [Fact]
    public void Applies_All_Given_Delegates()
    {
        var container = new McpServerDelegates();
        var server = new ExposeSetHandlersServer();

        container.ListToolsHandler = (p, c) => Task.FromResult(new ListToolsResult());
        container.CallToolHandler = (p, c) => Task.FromResult(new CallToolResponse());
        container.ListPromptsHandler = (p, c) => Task.FromResult(new ListPromptsResult());
        container.GetPromptHandler = (p, c) => Task.FromResult(new GetPromptResult());
        container.ListResourcesHandler = (p, c) => Task.FromResult(new ListResourcesResult());
        container.ReadResourceHandler = (p, c) => Task.FromResult(new ReadResourceResult());
        container.GetCompletionHandler = (p, c) => Task.FromResult(new CompleteResult());
        container.SubscribeToResourcesHandler = (s, c) => Task.CompletedTask;
        container.UnsubscribeFromResourcesHandler = (s, c) => Task.CompletedTask;

        container.Apply(server);

        Assert.Equal(container.ListToolsHandler, server.Handlers[OperationNames.ListTools]);
        Assert.Equal(container.CallToolHandler, server.Handlers[OperationNames.CallTool]);
        Assert.Equal(container.ListPromptsHandler, server.Handlers[OperationNames.ListPrompts]);
        Assert.Equal(container.GetPromptHandler, server.Handlers[OperationNames.GetPrompt]);
        Assert.Equal(container.ListResourcesHandler, server.Handlers[OperationNames.ListResources]);
        Assert.Equal(container.ReadResourceHandler, server.Handlers[OperationNames.ReadResource]);
        Assert.Equal(container.GetCompletionHandler, server.Handlers[OperationNames.GetCompletion]);
        Assert.Equal(container.SubscribeToResourcesHandler, server.Handlers[OperationNames.SubscribeToResources]);
        Assert.Equal(container.UnsubscribeFromResourcesHandler, server.Handlers[OperationNames.UnsubscribeFromResources]);
    }

    private sealed class ExposeSetHandlersServer : IMcpServer
    {
        public Dictionary<string, Delegate> Handlers = [];

        public void SetOperationHandler(string operationName, Delegate handler) => Handlers[operationName] = handler;

        public ValueTask DisposeAsync() => default;
        public bool IsInitialized => throw new NotImplementedException();
        public ClientCapabilities? ClientCapabilities => throw new NotImplementedException();
        public Implementation? ClientInfo => throw new NotImplementedException();
        public IServiceProvider? ServiceProvider => throw new NotImplementedException();
        public void AddNotificationHandler(string method, Func<JsonRpcNotification, Task> handler) => throw new NotImplementedException();
        public Task SendMessageAsync(IJsonRpcMessage message, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<T> SendRequestAsync<T>(JsonRpcRequest request, CancellationToken cancellationToken) where T : class => throw new NotImplementedException();
        public Task StartAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
