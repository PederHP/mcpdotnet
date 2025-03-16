using McpDotNet.Configuration;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace McpDotNet.Tests.Configuration;

public class McpServerBuilderExtensionsHandlerTests
{
    private readonly Mock<IMcpServerBuilder> _builder;
    private readonly ServiceCollection _services;

    public McpServerBuilderExtensionsHandlerTests()
    {
        _services = new ServiceCollection();
        _builder = new Mock<IMcpServerBuilder>();
        _builder.SetupGet(b => b.Services).Returns(_services);
    }

    [Fact]
    public void WithListToolsHandler_Sets_Handler()
    {
        Func<RequestContext<ListToolsRequestParams>, CancellationToken, Task<ListToolsResult>> handler = (context, token) => Task.FromResult(new ListToolsResult());

        _builder.Object.WithListToolsHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.ListToolsHandler);
    }

    [Fact]
    public void WithCallToolHandler_Sets_Handler()
    {
        Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>> handler = (context, token) => Task.FromResult(new CallToolResponse());

        _builder.Object.WithCallToolHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.CallToolHandler);
    }

    [Fact]
    public void WithListPromptsHandler_Sets_Handler()
    {
        Func<RequestContext<ListPromptsRequestParams>, CancellationToken, Task<ListPromptsResult>> handler = (context, token) => Task.FromResult(new ListPromptsResult());

        _builder.Object.WithListPromptsHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.ListPromptsHandler);
    }

    [Fact]
    public void WithGetPromptHandler_Sets_Handler()
    {
        Func<RequestContext<GetPromptRequestParams>, CancellationToken, Task<GetPromptResult>> handler = (context, token) => Task.FromResult(new GetPromptResult());

        _builder.Object.WithGetPromptHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.GetPromptHandler);
    }

    [Fact]
    public void WithListResourcesHandler_Sets_Handler()
    {
        Func<RequestContext<ListResourcesRequestParams>, CancellationToken, Task<ListResourcesResult>> handler = (context, token) => Task.FromResult(new ListResourcesResult());

        _builder.Object.WithListResourcesHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.ListResourcesHandler);
    }

    [Fact]
    public void WithReadResourceHandler_Sets_Handler()
    {
        Func<RequestContext<ReadResourceRequestParams>, CancellationToken, Task<ReadResourceResult>> handler = (context, token) => Task.FromResult(new ReadResourceResult());

        _builder.Object.WithReadResourceHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.ReadResourceHandler);
    }

    [Fact]
    public void WithGetCompletionHandler_Sets_Handler()
    {
        Func<RequestContext<CompleteRequestParams>, CancellationToken, Task<CompleteResult>> handler = (context, token) => Task.FromResult(new CompleteResult());

        _builder.Object.WithGetCompletionHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.GetCompletionHandler);
    }

    [Fact]
    public void WithSubscribeToResourcesHandler_Sets_Handler()
    {
        Func<RequestContext<string>, CancellationToken, Task> handler = (context, token) => Task.CompletedTask;

        _builder.Object.WithSubscribeToResourcesHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.SubscribeToResourcesHandler);
    }

    [Fact]
    public void WithUnsubscribeFromResourcesHandler_Sets_Handler()
    {
        Func<RequestContext<string>, CancellationToken, Task> handler = (context, token) => Task.CompletedTask;

        _builder.Object.WithUnsubscribeFromResourcesHandler(handler);

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.Equal(handler, options.UnsubscribeFromResourcesHandler);
    }
}
