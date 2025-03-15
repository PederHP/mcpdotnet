using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using System.Linq;
using McpDotNet;
using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging.Abstractions;

internal class Program
{
    private static async Task<IMcpClient> GetMcpClientAsync()
    {

        McpClientOptions options = new()
        {
            ClientInfo = new() { Name = "SimpleToolsConsole", Version = "1.0.0" }
        };

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

        var factory = new McpClientFactory(
            [config],
            options,
            NullLoggerFactory.Instance
        );

        return await factory.GetClientAsync("everything");
    }

    private static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Initializing MCP 'everything' server");
            var client = await GetMcpClientAsync();
            Console.WriteLine("MCP 'everything' server initialized");
            Console.WriteLine("Listing tools...");
            var tools = await client.ListToolsAsync().ToListAsync();
            var anthropicTools = tools.ToAnthropicTools();
            Console.WriteLine("Tools available:");
            foreach (var tool in anthropicTools)
            {
                Console.WriteLine("  " + tool.Function.Name);
            }

            Console.WriteLine("Starting chat with Claude Haiku 3.5...");
            using var antClient = new AnthropicClient(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!);

            Console.WriteLine("Asking Claude to call the Echo Tool...");

            var messages = new List<Message>
            {
                new Message(RoleType.User, "Please call the echo tool with the string 'Hello MCP!' and show me the echoed response.")
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude35Haiku,
                Stream = false,
                Temperature = 0.5m,
                Tools = anthropicTools,
                System = [new SystemMessage("You will be helping the user test MCP server tool call functionality. Remember that the user cannot see your tool calls or tool results.")]
            };

            // If the server provides instructions, add them as the system prompt
            if (!string.IsNullOrEmpty(client.ServerInstructions))
            {
                parameters.System.Add(new SystemMessage(client.ServerInstructions));
            }

            var res = await antClient.Messages.GetClaudeMessageAsync(parameters);

            messages.Add(res.Message);

            foreach (var toolCall in res.ToolCalls)
            {
                var response = await client.CallToolAsync(toolCall.Name, toolCall.Arguments?.ToMCPArguments() ?? []);

                messages.Add(new Message(toolCall, response.Content[0].Text));
            }

            var finalResult = await antClient.Messages.GetClaudeMessageAsync(parameters);
            Console.WriteLine("Final result: " + finalResult.Message.ToString());

            Console.WriteLine();

            Console.WriteLine("Chat with Claude Haiku 3.5 complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred: " + ex.Message);
        }
    }
}
