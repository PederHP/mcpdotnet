using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Constants;

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
            TransportType = "stdio",
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
            var tools = await client.ListToolsAsync();
            var anthropicTools = tools.Tools.ToAnthropicTools();
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
                new Message(RoleType.User, "Please call the echo tool with the string 'Hello MCP!' and give me the response as-is.")
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 2048,
                Model = AnthropicModels.Claude35Haiku,
                Stream = false,
                Temperature = 1.0m,
                Tools = anthropicTools
            };
            var res = await antClient.Messages.GetClaudeMessageAsync(parameters);

            messages.Add(res.Message);

            foreach (var toolCall in res.ToolCalls)
            {
                var response = await client.CallToolAsync(toolCall.Name, toolCall.Arguments?.ToMCPArguments());

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
