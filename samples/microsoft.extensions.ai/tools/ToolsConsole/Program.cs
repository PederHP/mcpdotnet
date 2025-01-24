using McpDotNet.Client;
using McpDotNet.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using OpenAI.Chat;
using SimpleToolsConsole;

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
            var mappedTools = tools.Tools.Select(t => t.ToAITool(client)).ToList();
            Console.WriteLine("Tools available:");
            foreach (var tool in mappedTools)
            {
                Console.WriteLine("  " + tool);
            }

            Console.WriteLine("Starting chat with GPT-4o-mini...");

            // Note: We use then Microsoft.Extensions.AI.OpenAI client here, but it could be any other MEAI client.
            // Provide your own OPENAI_API_KEY via an environment variable, secret or file-based appsettings. Do not hardcode it.
            IChatClient openaiClient = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                .AsChatClient("gpt-4o-mini");

            // Note: To use the ChatClientBuilder you need to install the Microsoft.Extensions.AI package
            IChatClient chatClient = new ChatClientBuilder(openaiClient)
                .UseFunctionInvocation()
                .Build();

            // Create message list
            IList<Microsoft.Extensions.AI.ChatMessage> messages = new List<Microsoft.Extensions.AI.ChatMessage>();
            // Add a system message
            messages.Add(new(ChatRole.System, "You are a helpful assistant, helping us test MCP server functionality."));
            // If MCP server provides instructions, add them as an additional system message (you could also add it as a content part)
            if (!string.IsNullOrEmpty(client.ServerInstructions))
            {
                messages.Add(new(ChatRole.System, client.ServerInstructions));
            }
            // Add a user message
            messages.Add(new(ChatRole.User, "Please call the echo tool with the string 'Hello MCP!' and give me the response as-is."));

            // Call the chat client
            Console.WriteLine("Asking GPT-4o-mini to call the Echo Tool...");
            var response = chatClient.CompleteStreamingAsync(
                    messages,
                    new() { Tools = mappedTools });

            await foreach (var update in response)
            {
                Console.Write(update);
            }
            Console.WriteLine();

            Console.WriteLine("Chat with GPT-4o-mini complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error occurred: " + ex.Message);
        }
    }
}