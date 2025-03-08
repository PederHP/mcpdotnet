using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Tests;

public class ClientIntegrationTests : IClassFixture<ClientIntegrationTestFixture>
{
    private readonly ClientIntegrationTestFixture _fixture;

    public ClientIntegrationTests(ClientIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConnectAndPing_Stdio_EverythingServer()
    {
        // Arrange

        // Act
        var client = await _fixture.Factory.GetClientAsync("everything");
        await client.PingAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task Connect_EverythingServer_ShouldProvideServerFields()
    {
        // Arrange

        // Act
        var client = await _fixture.Factory.GetClientAsync("everything");

        // Assert
        Assert.NotNull(client.ServerCapabilities);
        Assert.NotNull(client.ServerInfo);

        // Note: Comment the below assertion back when the everything server is updated to provide instructions
        // Assert.NotNull(client.ServerInstructions);
    }

    [Fact]
    public async Task ListTools_Stdio_EverythingServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");
        var tools = await client.ListToolsAsync();

        // assert
        Assert.NotNull(tools);
        Assert.NotEmpty(tools.Tools);
        // We could add more specific assertions about expected tools
    }

    [Fact]
    public async Task CallTool_Stdio_EchoServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");
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
    public async Task ListPrompts_Stdio_EverythingServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");
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
        var client = await _fixture.Factory.GetClientAsync("everything");
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
        var client = await _fixture.Factory.GetClientAsync("everything");
        var arguments = new Dictionary<string, object>
        {
            { "temperature", "0.7" },
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
        var client = await _fixture.Factory.GetClientAsync("everything");
        await Assert.ThrowsAsync<McpClientException>(() =>
            client.GetPromptAsync("non_existent_prompt", null, CancellationToken.None));
    }

    [Fact]
    public async Task ListResources_Stdio_EverythingServer()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");

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
        var client = await _fixture.Factory.GetClientAsync("everything");
        // Odd numbered resources are text in the everything server (despite the docs saying otherwise)
        // 1 is index 0, which is "even" in the 0-based index
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
        var client = await _fixture.Factory.GetClientAsync("everything");
        // Even numbered resources are binary in the everything server (despite the docs saying otherwise)
        // 2 is index 1, which is "odd" in the 0-based index
        var result = await client.ReadResourceAsync("test://static/resource/2", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.NotNull(result.Contents[0].Blob);
    }

    /// <summary>
    /// Note that as of 19th January 2025, the everything server published to npx does not support prompt completion.
    /// However, the prompt completion is implemented in the github.com/modelcontextprotocol/servers repository.
    /// You can clone this repo, and build locally then change the config to use a local symlink instead of npx until this is fixed, if you wish to run this test.
    /// TransportOptions = new Dictionary<string, string>
    /// {
    ///    ["command"] = "npx",
    ///    ["arguments"] = "mcp-server-everything" // changed from "-y @modelcontextprotocol/server-everything"
    /// }
    /// </summary>
    [Fact]
    public async Task GetCompletion_Stdio_ResourceReference()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");
        var result = await client.GetCompletionAsync(new Reference
        {
            Type = "ref/resource",
            Uri = "test://static/resource/1"
        },
            "argument_name", "1",
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Single(result.Completion.Values);
        Assert.Equal("1", result.Completion.Values[0]);
    }

    /// <summary>
    /// Note that as of 19th January 2025, the everything server published to npx does not support prompt completion.
    /// However, the prompt completion is implemented in the github.com/modelcontextprotocol/servers repository.
    /// You can clone this repo, and build locally then change the config to use a local symlink instead of npx until this is fixed, if you wish to run this test.
    /// TransportOptions = new Dictionary<string, string>
    /// {
    ///    ["command"] = "npx",
    ///    ["arguments"] = "mcp-server-everything" // changed from "-y @modelcontextprotocol/server-everything"
    /// }
    /// </summary>
    [Fact]
    public async Task GetCompletion_Stdio_PromptReference()
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync("everything");
        var result = await client.GetCompletionAsync(new Reference
        {
            Type = "ref/prompt",
            Name = "irrelevant"
        },
            argumentName: "style", argumentValue: "fo",
            CancellationToken.None
        );

        Assert.NotNull(result);
        Assert.Single(result.Completion.Values);
        Assert.Equal("formal", result.Completion.Values[0]);
    }

    [Fact]
    public async Task Sampling_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything",
            }
        };

        var options = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" },
            Capabilities = new ClientCapabilities
            {
                Sampling = new()
            }
        };

        var factory = new McpClientFactory(
            [config],
            options,
            _fixture.LoggerFactory
        );
        var client = await factory.GetClientAsync("everything");

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

    [Fact]
    public async Task Roots_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "everything",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var options = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" },
            Capabilities = new ClientCapabilities
            {
                Roots = new()
            }
        };

        var rootsHandlerCalls = 0;
        var testRoots = new List<Root>
        {
            new() { Uri = "file:///test/root1", Name = "Test Root 1" },
            new() { Uri = "file:///test/root2", Name = "Test Root 2" }
        };

        var factory = new McpClientFactory(
            [config],
            options,
            _fixture.LoggerFactory
        );
        var client = await factory.GetClientAsync("everything");

        // Set up the roots handler
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        client.RootsHandler = async (request, ct) =>
        {
            rootsHandlerCalls++;
            return new ListRootsResult
            {
                Roots = testRoots
            };
        };
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        // Connect
        await client.ConnectAsync(CancellationToken.None);

        // assert
        // nothing to assert, no servers implement roots, so we if no exception is thrown, it's a success
        Assert.True(true);
    }

    [Fact]
    public async Task Notifications_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "everything",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var options = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var factory = new McpClientFactory([config], options, _fixture.LoggerFactory);
        var client = await factory.GetClientAsync("everything");

        await client.ConnectAsync();

        // Verify we can send notifications without errors
        await client.SendNotificationAsync(NotificationMethods.RootsUpdatedNotification);
        await client.SendNotificationAsync("test/notification", new { test = true });

        // assert
        // no response to check, if no exception is thrown, it's a success
        Assert.True(true);
    }

    [Fact]
    public async Task CallTool_Stdio_MemoryServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "memory",
            Name = "memory",
            TransportType = TransportTypes.StdIo,
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-memory"
            }
        };

        var options = new McpClientOptions
        {
            ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
        };

        var factory = new McpClientFactory([config], options, _fixture.LoggerFactory);
        var client = await factory.GetClientAsync("memory");

        await client.ConnectAsync();

        // act
        var result = await client.CallToolAsync(
            "read_graph",
            [],
            CancellationToken.None
        );

        // assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        var textContent = Assert.Single(result.Content, c => c.Type == "text");
    }
}
