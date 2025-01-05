using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace McpDotNet.Tests;

public class IntegrationTests
{
    private readonly McpClientOptions _defaultOptions = new()
    {
        ClientInfo = new() { Name = "IntegrationTestClient", Version = "1.0.0" }
    };

    [Fact]
    public async Task ConnectAndPing_Stdio_EverythingServer()
    {
        // Arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything",
            }
        };

        // Inject the mock transport into the factory
        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        // Act
        var client = await factory.GetClientAsync("everything");
        await client.PingAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public async Task ListTools_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        // act
        var client = await factory.GetClientAsync("everything");
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        // act
        var client = await factory.GetClientAsync("everything");
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
        var result = await client.GetPromptAsync("simple_prompt", null, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public async Task GetPrompt_Stdio_ComplexPrompt()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
        await Assert.ThrowsAsync<McpClientException>(() =>
            client.GetPromptAsync("non_existent_prompt", null, CancellationToken.None));
    }

    [Fact]
    public async Task ListResources_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");

        List<Resource> allResources = new();
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
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
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
            TransportOptions = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["arguments"] = "-y @modelcontextprotocol/server-everything"
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            _defaultOptions
        );

        var client = await factory.GetClientAsync("everything");
        // Even numbered resources are binary in the everything server (despite the docs saying otherwise)
        // 2 is index 1, which is "odd" in the 0-based index
        var result = await client.ReadResourceAsync("test://static/resource/2", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Contents);
        Assert.NotNull(result.Contents[0].Blob);
    }

    [Fact]
    public async Task Sampling_Stdio_EverythingServer()
    {
        // arrange
        var config = new McpServerConfig
        {
            Id = "everything",
            Name = "Everything",
            TransportType = "stdio",
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
                Sampling = new()
            }
        };

        var factory = new McpClientFactory(
            new[] { config },
            options
        );
        var client = await factory.GetClientAsync("everything");

        // Set up the sampling handler
        int samplingHandlerCalls = 0;
        client.SamplingHandler = async (_) =>
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
        Assert.Contains("Test response", textContent.Text);
    }
}
