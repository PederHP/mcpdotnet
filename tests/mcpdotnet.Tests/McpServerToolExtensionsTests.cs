using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace McpDotNet.Tests;

public class McpServerToolExtensionsTests
{
    [Fact]
    public void Adds_Tools_To_Server()
    {
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoTool));

        Assert.NotNull(server.Object.ListToolsHandler);
        Assert.NotNull(server.Object.CallToolHandler);
    }

    [Fact]
    public async Task Can_List_Registered_Tool()
    {
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoTool));

        var result = await server.Object.ListToolsHandler!(new Protocol.Types.ListToolsRequestParams { }, CancellationToken.None);
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
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoTool));

        var result = await server.Object.CallToolHandler!(new Protocol.Types.CallToolRequestParams { Name = "Echo", Arguments = new() { { "message", "Peter" } } }, CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Dependency_Injection()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);

        serviceScopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);
        serviceProvider.Setup(x => x.GetService(typeof(IDependendService))).Returns(new Mock<IDependendService>().Object);

        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();
        server.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoToolWithDi));

        var result = await server.Object.CallToolHandler!(new Protocol.Types.CallToolRequestParams { Name = "Echo", Arguments = new() { { "message", "Peter" } } }, CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);

        serviceProvider.Verify(x => x.GetService(typeof(IDependendService)), Times.Once);
    }

    [Fact]
    public async Task Throws_Exception_On_Unknown_Tool()
    {
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoTool));

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await server.Object.CallToolHandler!(new Protocol.Types.CallToolRequestParams { Name = "NotRegisteredTool" }, CancellationToken.None));
        Assert.Equal("Unknown tool: NotRegisteredTool", exception.Message);
    }

    [Fact]
    public async Task Throws_Exception_Missing_Parameter()
    {
        var server = new Mock<IMcpServer>();
        server.SetupAllProperties();

        McpServerToolExtensions.AddTools(server.Object, typeof(EchoTool));

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await server.Object.CallToolHandler!(new Protocol.Types.CallToolRequestParams { Name = "Echo" }, CancellationToken.None));
        Assert.Equal("Missing required argument 'message'.", exception.Message);
    }

    private static class EchoTool
    {
        [McpTool(Description = "Echoes the input back to the client.")]
        public static string Echo([McpParameter(true, Description = "the echoes message")] string message)
        {
            return "hello " + message;
        }

        [McpTool(Name = "double_echo", Description = "Echoes the input back to the client.")]
        public static string Echo2([McpParameter(true)] string message)
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

        [McpTool(Description = "Echoes the input back to the client.")]
        public Task<string> Echo([McpParameter(true)] string message)
        {
            return Task.FromResult("hello " + message);
        }
    }
}
