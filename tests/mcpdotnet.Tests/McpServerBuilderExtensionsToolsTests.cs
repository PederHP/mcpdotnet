using McpDotNet.Configuration;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.ComponentModel;

namespace McpDotNet.Tests;

public class McpServerBuilderExtensionsToolsTests
{
    private readonly Mock<IMcpServerBuilder> _builder;
    private readonly ServiceCollection _services;

    public McpServerBuilderExtensionsToolsTests()
    {
        _services = new ServiceCollection();
        _builder = new Mock<IMcpServerBuilder>();
        _builder.SetupGet(b => b.Services).Returns(_services);
    }

    [Fact]
    public void Adds_Tools_To_Server()
    {
        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        Assert.NotNull(options.ListToolsHandler);
        Assert.NotNull(options.CallToolHandler);
    }

    [Fact]
    public async Task Can_List_Registered_Tool()
    {
        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        var result = await options.ListToolsHandler!(new(Mock.Of<IMcpServer>(), new()), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Tools);

        var tool = result.Tools[0];
        Assert.Equal("Echo", tool.Name);
        Assert.Equal("Echoes the input back to the client.", tool.Description);
        Assert.NotNull(tool.InputSchema);
        Assert.Equal("object", tool.InputSchema.Type);
        Assert.NotNull(tool.InputSchema.Properties);
        Assert.NotEmpty(tool.InputSchema.Properties);
        Assert.Contains("message", tool.InputSchema.Properties);
        Assert.Equal("string", tool.InputSchema.Properties["message"].Type);
        Assert.Equal("the echoes message", tool.InputSchema.Properties["message"].Description);
        Assert.NotNull(tool.InputSchema.Required);
        Assert.NotEmpty(tool.InputSchema.Required);
        Assert.Contains("message", tool.InputSchema.Required);

        tool = result.Tools[1];
        Assert.Equal("double_echo", tool.Name);
        Assert.Equal("Echoes the input back to the client.", tool.Description);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool()
    {
        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "Echo", Arguments = new() { { "message", "Peter" } } }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Dependency_Injection()
    {
        bool dependencyInjectionCalled = false;
        _services.AddTransient<IDependendService>(sp =>
        {
            dependencyInjectionCalled = true;
            return Mock.Of<IDependendService>();
        });

        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoToolWithDi));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        var mcpServer = new Mock<IMcpServer>();
        mcpServer.SetupGet(s => s.ServiceProvider).Returns(serviceProvider);

        var result = await options.CallToolHandler!(new(mcpServer.Object, new() { Name = "Echo", Arguments = new() { { "message", "Peter" } } }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);

        Assert.True(dependencyInjectionCalled);
    }

    [Fact]
    public async Task Throws_Exception_On_Unknown_Tool()
    {
        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "NotRegisteredTool" }), CancellationToken.None));
        Assert.Equal("Unknown tool: NotRegisteredTool", exception.Message);
    }

    [Fact]
    public async Task Throws_Exception_Missing_Parameter()
    {
        McpServerBuilderExtensions.WithTools(_builder.Object, typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerDelegates>>().Value;

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "Echo" }), CancellationToken.None));
        Assert.Equal("Missing required argument 'message'.", exception.Message);
    }

    private static class EchoTool
    {
        [McpTool, Description("Echoes the input back to the client.")]
        public static string Echo([Description("the echoes message")] string message)
        {
            return "hello " + message;
        }

        [McpTool("double_echo"), Description("Echoes the input back to the client.")]
        public static string Echo2(string message)
        {
            return "hello hello" + message;
        }
    }

    public interface IDependendService
    {
    }

    private class EchoToolWithDi
    {
        public EchoToolWithDi(IDependendService service)
        {

        }

        [McpTool, Description("Echoes the input back to the client.")]
#pragma warning disable CA1822 // Mark members as static
        public Task<string> Echo(string message)
#pragma warning restore CA1822 // Mark members as static
        {
            return Task.FromResult("hello " + message);
        }
    }
}
