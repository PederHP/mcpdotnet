using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;

namespace McpDotNet.Extensions.AI.Tests;

[Trait("Execution", "Manual")]
public class IntegrationTests
{
    private string _openAIKey = Environment.GetEnvironmentVariable("Provide your own key when running the test. Do not commit it.")!;

    private static McpServerConfig GetEverythingServerConfig()
    {
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

        return config;
    }

    private static async Task<IMcpClient> GetMcpClientAsync()
    {
        McpClientOptions options = new()
        {
            ClientInfo = new() { Name = "McpDotNet.Extensions.AI.Tests", Version = "1.0.0" }
        };

        var factory = new McpClientFactory(
            [GetEverythingServerConfig()],
            options,
            NullLoggerFactory.Instance
        );

        return await factory.GetClientAsync("everything");
    }

    [Fact]
    public async Task IntegrateWithMeai_UsingEverythingServer_ToolsAreProperlyCalled()
    {
        var client = await GetMcpClientAsync();
        var tools = await client.ListToolsAsync();
        var mappedTools = tools.Tools.Select(t => t.ToAITool(client)).ToList();

        IChatClient openaiClient = new OpenAIClient(_openAIKey)
            .AsChatClient("gpt-4o-mini");

        IChatClient chatClient = new ChatClientBuilder(openaiClient)
            .UseFunctionInvocation()
            .Build();

        // Create message list
        IList<ChatMessage> messages =
        [
            // Add a system message
            new(ChatRole.System, "You are a helpful assistant, helping us test MCP server functionality."),
        ];
        // If MCP server provides instructions, add them as an additional system message (you could also add it as a content part)
        if (!string.IsNullOrEmpty(client.ServerInstructions))
        {
            messages.Add(new(ChatRole.System, client.ServerInstructions));
        }
        // Add a user message
        messages.Add(new(ChatRole.User, "Please call the echo tool with the string 'Hello MCP!' and output the response ad verbatim."));

        // Call the chat client
        Console.WriteLine("Asking GPT-4o-mini to call the Echo Tool...");
        var response = await chatClient.GetResponseAsync(
                messages,
                new() { Tools = mappedTools, Temperature = 0 });

        // Assert
        Assert.Equal("Echo: Hello MCP!", response.Text);
    }

    [Fact]
    public async Task IntegrateWithMeai_UsingEverythingServer_AndSessionScope_ToolsAreProperlyCalled()
    {
        await using var sessionScope = await McpSessionScope.CreateAsync(GetEverythingServerConfig());

        IChatClient openaiClient = new OpenAIClient(_openAIKey)
            .AsChatClient("gpt-4o-mini");

        IChatClient chatClient = new ChatClientBuilder(openaiClient)
            .UseFunctionInvocation()
            .Build();

        // Create message list
        IList<ChatMessage> messages =
        [
            // Add a system message
            new(ChatRole.System, "You are a helpful assistant, helping us test MCP server functionality."),
        ];
        // If MCP server provides instructions, add them as an additional system message (you could also add it as a content part)
        foreach (var serverInstruction in sessionScope.ServerInstructions)
        {
            messages.Add(new(ChatRole.System, serverInstruction));
        }
        // Add a user message
        messages.Add(new(ChatRole.User, "Please call the echo tool with the string 'Hello MCP!' and output the response ad verbatim."));

        // Call the chat client
        Console.WriteLine("Asking GPT-4o-mini to call the Echo Tool...");
        var response = await chatClient.GetResponseAsync(
                messages,
                new() { Tools = sessionScope.Tools, Temperature = 0 });

        // Assert
        Assert.Equal("Echo: Hello MCP!", response.Text);
    }
}
