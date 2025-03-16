using System.ComponentModel;
using System.Text.Json;
using McpDotNet.Configuration;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace McpDotNet.Tests.Configuration;

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
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        Assert.NotNull(options.ListToolsHandler);
        Assert.NotNull(options.CallToolHandler);
    }

    [Fact]
    public async Task Can_List_Registered_Tool()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

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
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "Echo", Arguments = new() { { "message", "Peter" } } }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Array_Result()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "EchoArray", Arguments = new() { { "message", "Peter" } } }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("hello Peter", result.Content[0].Text);
        Assert.Equal("hello2 Peter", result.Content[1].Text);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Null_Result()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnNull" }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("null", result.Content[0].Text);
        Assert.Empty(result.Content[0].Type);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Json_Result()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnJson" }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("{\"SomeProp\": false}", result.Content[0].Text);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Int_Result()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnInteger" }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("5", result.Content[0].Text);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_And_Pass_Cancellation_Token()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnCancellationToken" }), token);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal(token.GetHashCode().ToString(), result.Content[0].Text);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_And_Returns_Cancelled_Response()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await cts.CancelAsync();

        var action = async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnCancellationToken" }), token);

        await Assert.ThrowsAsync<OperationCanceledException>(action);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_And_Pass_ComplexType()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "EchoComplex", Arguments = new() { { "complex", JsonDocument.Parse("{\"Name\": \"Peter\", \"Age\": 25}").RootElement } } }), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotNull(result.Content);
        Assert.NotEmpty(result.Content);

        Assert.Equal("Peter", result.Content[0].Text);
        Assert.Equal("text", result.Content[0].Type);
    }

    [Fact]
    public async Task Throws_Exception_When_Tool_Fails()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var action = async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "ReturnError" }), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("Test error", exception.Message);
    }

    [Fact]
    public async Task Can_Call_Registered_Tool_With_Dependency_Injection()
    {
        bool dependencyInjectionCalled = false;
        _services.AddTransient(sp =>
        {
            dependencyInjectionCalled = true;
            return Mock.Of<IDependendService>();
        });

        _builder.Object.WithTool<EchoToolWithDi>();

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

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
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "NotRegisteredTool" }), CancellationToken.None));
        Assert.Equal("Unknown tool: NotRegisteredTool", exception.Message);
    }

    [Fact]
    public async Task Throws_Exception_Missing_Parameter()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var exception = await Assert.ThrowsAsync<McpServerException>(async () => await options.CallToolHandler!(new(Mock.Of<IMcpServer>(), new() { Name = "Echo" }), CancellationToken.None));
        Assert.Equal("Missing required argument 'message'.", exception.Message);
    }

    [Fact]
    public void Throws_Exception_For_Null_Types()
    {
        var action = () => _builder.Object.WithTools(toolTypes: null!);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("At least one tool type must be provided. (Parameter 'toolTypes')", exception.Message);
    }

    [Fact]
    public void Throws_Exception_For_Empty_Types()
    {
        var action = () => _builder.Object.WithTools(toolTypes: []);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("At least one tool type must be provided. (Parameter 'toolTypes')", exception.Message);
    }

    [Fact]
    public async Task Register_Tools_From_Current_Assembly()
    {
        _builder.Object.WithTools();

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.ListToolsHandler!(new(Mock.Of<IMcpServer>(), new()), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Tools);

        var tool = result.Tools[0];
        Assert.Equal("Echo", tool.Name);
    }

    [Fact]
    public void Throws_Exception_When_No_Tools_Are_Found_In_Given_Assembly()
    {
        var action = () => _builder.Object.WithToolsFromAssembly(typeof(Mock).Assembly);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal("No types with marked methods found in the assembly. (Parameter 'assembly')", exception.Message);
    }

    [Fact]
    public async Task Recognizes_Parameter_Types()
    {
        _builder.Object.WithTools(typeof(EchoTool));

        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<McpServerHandlers>>().Value;

        var result = await options.ListToolsHandler!(new(Mock.Of<IMcpServer>(), new()), CancellationToken.None);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Tools);

        var tool = result.Tools.First(t => t.Name == "TestTool");
        Assert.Equal("TestTool", tool.Name);
        Assert.Null(tool.Description);
        Assert.NotNull(tool.InputSchema);
        Assert.Equal("object", tool.InputSchema.Type);
        Assert.NotNull(tool.InputSchema.Properties);
        Assert.NotEmpty(tool.InputSchema.Properties);

        Assert.Contains("number", tool.InputSchema.Properties);
        Assert.Equal("number", tool.InputSchema.Properties["number"].Type);

        Assert.Contains("otherNumber", tool.InputSchema.Properties);
        Assert.Equal("number", tool.InputSchema.Properties["otherNumber"].Type);

        Assert.Contains("someCheck", tool.InputSchema.Properties);
        Assert.Equal("boolean", tool.InputSchema.Properties["someCheck"].Type);

        Assert.Contains("someDate", tool.InputSchema.Properties);
        Assert.Equal("string", tool.InputSchema.Properties["someDate"].Type);

        Assert.Contains("someOtherDate", tool.InputSchema.Properties);
        Assert.Equal("string", tool.InputSchema.Properties["someOtherDate"].Type);

        Assert.Contains("data", tool.InputSchema.Properties);
        Assert.Equal("array", tool.InputSchema.Properties["data"].Type);

        Assert.Contains("complexObject", tool.InputSchema.Properties);
        Assert.Equal("object", tool.InputSchema.Properties["complexObject"].Type);
    }

    [McpToolType]
    public static class EchoTool
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

        [McpTool]
        public static string TestTool(int number, double otherNumber, bool someCheck, DateTime someDate, DateTimeOffset someOtherDate, string[] data, ComplexObject complexObject)
        {
            return "hello hello";
        }

        [McpTool]
        public static string[] EchoArray(string message)
        {
            return ["hello " + message, "hello2 " + message];
        }

        [McpTool]
        public static string? ReturnNull()
        {
            return null;
        }

        [McpTool]
        public static JsonElement ReturnJson()
        {
            return JsonDocument.Parse("{\"SomeProp\": false}").RootElement;
        }

        [McpTool]
        public static int ReturnInteger()
        {
            return 5;
        }

        [McpTool]
        public static string ReturnError()
        {
            throw new InvalidOperationException("Test error");
        }

        [McpTool]
        public static int ReturnCancellationToken(CancellationToken cancellationToken)
        {
            return cancellationToken.GetHashCode();
        }

        [McpTool]
        public static string EchoComplex(ComplexObject complex)
        {
            return complex.Name!;
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

    public class ComplexObject
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }
}
