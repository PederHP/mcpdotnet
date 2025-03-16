﻿using McpDotNet.Client;
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

    public static IEnumerable<object[]> GetClients()
    {
        yield return ["everything"];
        yield return ["test_server"];
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ConnectAndPing_Stdio(string clientId)
    {
        // Arrange

        // Act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        await client.PingAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(client);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task Connect_ShouldProvideServerFields(string clientId)
    {
        // Arrange

        // Act
        var client = await _fixture.Factory.GetClientAsync(clientId);

        // Assert
        Assert.NotNull(client.ServerCapabilities);
        Assert.NotNull(client.ServerInfo);
        if (clientId != "everything")   // Note: Comment the below assertion back when the everything server is updated to provide instructions
            Assert.NotNull(client.ServerInstructions);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ListTools_Stdio(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        var tools = await client.ListToolsAsync().ToListAsync();

        // assert
        Assert.NotEmpty(tools);
        // We could add more specific assertions about expected tools
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task CallTool_Stdio_EchoServer(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
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

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ListPrompts_Stdio(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        var prompts = await client.ListPromptsAsync().ToListAsync();

        // assert
        Assert.NotEmpty(prompts);
        // We could add specific assertions for the known prompts
        Assert.Contains(prompts, p => p.Name == "simple_prompt");
        Assert.Contains(prompts, p => p.Name == "complex_prompt");
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task GetPrompt_Stdio_SimplePrompt(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        var result = await client.GetPromptAsync("simple_prompt", null, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task GetPrompt_Stdio_ComplexPrompt(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
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

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task GetPrompt_NonExistent_ThrowsException(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        await Assert.ThrowsAsync<McpClientException>(() =>
            client.GetPromptAsync("non_existent_prompt", null, CancellationToken.None));
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ListResources_Stdio(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);

        List<Resource> allResources = [];
        string? cursor = null;
        do
        {
            var resources = await client.ListResourcesAsync(cursor, CancellationToken.None);
            allResources.AddRange(resources.Resources);
            cursor = resources.NextCursor;
        }
        while (cursor != null);

        // The server provides 100 test resources
        Assert.Equal(100, allResources.Count);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ReadResource_Stdio_TextResource(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        // Odd numbered resources are text in the everything server (despite the docs saying otherwise)
        // 1 is index 0, which is "even" in the 0-based index
        var result = await client.ReadResourceAsync("test://static/resource/1", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.NotNull(result.Contents[0].Text);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task ReadResource_Stdio_BinaryResource(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
        // Even numbered resources are binary in the everything server (despite the docs saying otherwise)            
        // 2 is index 1, which is "odd" in the 0-based index
        var result = await client.ReadResourceAsync("test://static/resource/2", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result                                                    .Contents);
        Assert.NotNull(result.Contents[0].Blob);
    }

    // Not support by "everything" server version on npx
    [Fact]
    public async Task SubscribeResource_Stdio()
    {
        // arrange
        var clientId = "test_server";

        // act
        int counter = 0;
        var client = await _fixture.Factory.GetClientAsync(clientId);
        client.AddNotificationHandler(NotificationMethods.ResourceUpdatedNotification, async (notification) =>
        {
            var notificationParams = notification.Params;
            Assert.NotNull(notificationParams);
            ++counter;
            //Assert.Equal("test://static/resource/1", resource!.Uri);
        });
        await client.SubscribeToResourceAsync("test://static/resource/1", CancellationToken.None);

        // notifications happen every 5 seconds, so we wait for 10 seconds to ensure we get at least one notification
        await Task.Delay(10000);

        // assert
        Assert.True(counter > 0);
    }

    // Not support by "everything" server version on npx
    [Fact]
    public async Task UnsubscribeResource_Stdio()
    {
        // arrange
        var clientId = "test_server";

        // act
        int counter = 0;
        var client = await _fixture.Factory.GetClientAsync(clientId);
        client.AddNotificationHandler(NotificationMethods.ResourceUpdatedNotification, async (notification) =>
        {
            var notificationParams = notification.Params;
            Assert.NotNull(notificationParams);
            ++counter;
            //Assert.Equal("test://static/resource/1", resource!.Uri);
        });
        await client.SubscribeToResourceAsync("test://static/resource/1", CancellationToken.None);

        // notifications happen every 5 seconds, so we wait for 10 seconds to ensure we get at least one notification
        await Task.Delay(10000);

        // reset counter
        int counterAfterSubscribe = counter;
        
        // unsubscribe
        await client.UnsubscribeFromResourceAsync("test://static/resource/1", CancellationToken.None);
        counter = 0;

        // notifications happen every 5 seconds, so we wait for 10 seconds to ensure we would've gotten at least one notification
        await Task.Delay(10000);

        // assert
        Assert.True(counterAfterSubscribe > 0);
        Assert.True(counter == 0);
    }

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task GetCompletion_Stdio_ResourceReference(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
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

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task GetCompletion_Stdio_PromptReference(string clientId)
    {
        // arrange

        // act
        var client = await _fixture.Factory.GetClientAsync(clientId);
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

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task Sampling_Stdio(string clientId)
    {
        var client = await _fixture.Factory.GetClientAsync(clientId);

        // Set up the sampling handler
        int samplingHandlerCalls = 0;
        client.SetSamplingHandler((_, _) =>
        {
            samplingHandlerCalls++;
            return Task.FromResult(new CreateMessageResult
            {
                Model = "test-model",
                Role = "assistant",
                Content = new Content
                {
                    Type = "text",
                    Text = "Test response"
                }
            });
        });

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

    //[Theory]
    //[MemberData(nameof(GetClients))]
    //public async Task Roots_Stdio_EverythingServer(string clientId)
    //{       
    //    var rootsHandlerCalls = 0;
    //    var testRoots = new List<Root>
    //    {
    //        new() { Uri = "file:///test/root1", Name = "Test Root 1" },
    //        new() { Uri = "file:///test/root2", Name = "Test Root 2" }
    //    };

    //    var client = await _fixture.Factory.GetClientAsync(clientId);

    //    // Set up the roots handler
    //    client.SetRootsHandler((request, ct) =>
    //    {
    //        rootsHandlerCalls++;
    //        return Task.FromResult(new ListRootsResult
    //        {
    //            Roots = testRoots
    //        });
    //    });

    //    // Connect
    //    await client.ConnectAsync(CancellationToken.None);

    //    // assert
    //    // nothing to assert, no servers implement roots, so we if no exception is thrown, it's a success
    //    Assert.True(true);
    //}

    [Theory]
    [MemberData(nameof(GetClients))]
    public async Task Notifications_Stdio(string clientId)
    {
        var client = await _fixture.Factory.GetClientAsync(clientId);

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

        using var factory = new McpClientFactory([config], options, _fixture.LoggerFactory);
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
        Assert.Single(result.Content, c => c.Type == "text");

        await client.DisposeAsync();
    }
}
