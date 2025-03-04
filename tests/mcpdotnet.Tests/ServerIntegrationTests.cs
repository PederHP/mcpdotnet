using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Tests;

public class ServerIntegrationTests : IClassFixture<ServerIntegrationTestFixture>
{
    private readonly ServerIntegrationTestFixture _fixture;

    public ServerIntegrationTests(ServerIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConnectAndPing_Stdio_TestServer()
    {
        // Arrange
        
        // Act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        await client.PingAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Connect_TestServer_ShouldProvideServerFields()
    {
        // Arrange

        // Act
        var client = await _fixture.Factory.GetClientAsync("test_server");

        // Assert
        Assert.NotNull(client.ServerCapabilities);
        Assert.NotNull(client.ServerInfo);
        Assert.NotNull(client.ServerInstructions);
    }

    [Fact]
    public async Task ListTools_Stdio_TestServer()
    {
        // arrange
       
        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        var tools = await client.ListToolsAsync();

        // assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools.Tools);
    }

    [Fact]
    public async Task CallTool_Stdio_EchoServer()
    {
        // arrange
        
        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        var result = await client.CallToolAsync(
            "echo",
            new Dictionary<string, object>
            {
                ["message"] = "Hello MCP!"
            },
            CancellationToken.None
        );

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        var textContent = Assert.Single(result.Content, c => c.Type == "text");
        Assert.Equal("Echo: Hello MCP!", textContent.Text);
    }

    [Fact]
    public async Task ListResources_Stdio_TestServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");

        List<Resource> allResources = [];
        string? cursor = null;
        do
        {
            var resources = await client.ListResourcesAsync(cursor, CancellationToken.None);
            allResources.AddRange(resources.Resources);
            cursor = resources.NextCursor;
        }
        while (cursor != null);

        // The everything server provides 100 test resources
        Assert.Equal(100, allResources.Count);
    }

    [Fact]
    public async Task ReadResource_Stdio_TextResource()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        // Odd numbered resources are text in the everything server (despite the docs saying otherwise)
        // 1 is index 0, which is "even" in the 0-based index
        // We copied this oddity to the test server
        var result = await client.ReadResourceAsync("test://static/resource/1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.NotNull(result.Contents[0].Text);
    }

    [Fact]
    public async Task ReadResource_Stdio_BinaryResource()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        // Even numbered resources are binary in the everything server (despite the docs saying otherwise)
        // 2 is index 1, which is "odd" in the 0-based index
        // We copied this oddity to the test server
        var result = await client.ReadResourceAsync("test://static/resource/2", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.NotNull(result.Contents[0].Blob);
    }

    [Fact]
    public async Task ListPrompts_Stdio_TestServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        var prompts = await client.ListPromptsAsync();

        // assert
        Assert.NotNull(prompts);
        Assert.NotEmpty(prompts.Prompts);
        // We could add specific assertions for the known prompts
        Assert.Contains(prompts.Prompts, p => p.Name == "simple_prompt");
        Assert.Contains(prompts.Prompts, p => p.Name == "complex_prompt");
    }

    [Fact]
    public async Task GetPrompt_Stdio_SimplePrompt()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        var result = await client.GetPromptAsync("simple_prompt", null, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public async Task GetPrompt_Stdio_ComplexPrompt()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        var arguments = new Dictionary<string, object>
        {
            { "temperature", 0.7 },
            { "style", "formal" }
        };
        var result = await client.GetPromptAsync("complex_prompt", arguments, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public async Task GetPrompt_NonExistent_ThrowsException()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("test_server");
        await Assert.ThrowsAsync<McpClientException>(() =>
            client.GetPromptAsync("non_existent_prompt", null, CancellationToken.None));
    }

    [Fact]
    public async Task Sampling_Stdio_TestServer()
    {
        // arrange        
        var client = await _fixture.Factory.GetClientAsync("test_server");

        // Set up the sampling handler
        int samplingHandlerCalls = 0;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        client.SamplingHandler = async (_, _) =>
        {
            samplingHandlerCalls++;
            return new CreateMessageResult
            {
                Model = "test-model",
                Role = "assistant",
                Content = new Content
                {
                    Type = "text",
                    Text = "Test response"
                }
            };
        };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        // Call the server's sampleLLM tool which should trigger our sampling handler
        var result = await client.CallToolAsync(
            "sampleLLM",
            new Dictionary<string, object>
            {
                ["prompt"] = "Test prompt",
                ["maxTokens"] = 100
            }
        );

        // assert
        Assert.NotNull(result);
        var textContent = Assert.Single(result.Content);
        Assert.Equal("text", textContent.Type);
        Assert.False(string.IsNullOrEmpty(textContent.Text));
    }
}
