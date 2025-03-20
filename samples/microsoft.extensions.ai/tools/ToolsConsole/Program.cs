﻿using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.AI;
using OpenAI;

try
{
    Console.WriteLine("Initializing MCP 'everything' server");
    await using var client = await GetMcpClientAsync();

    Console.WriteLine("MCP 'everything' server initialized");
    Console.WriteLine("Listing tools...");
    var mappedTools = await client.ListToolsAsync().ToListAsync();
    Console.WriteLine("Tools available:");
    foreach (var tool in mappedTools)
    {
        Console.WriteLine($"  {tool}");
    }

    Console.WriteLine("Starting chat with GPT-4o-mini...");

    // Note: We use then Microsoft.Extensions.AI.OpenAI client here, but it could be any other MEAI client.
    // Provide your own OPENAI_API_KEY via an environment variable, secret or file-based appsettings. Do not hardcode it.
    IChatClient chatClient = new OpenAIClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
        .AsChatClient("gpt-4o-mini")
        .AsBuilder()
        .UseFunctionInvocation()
        .Build();

    // Create message list
    IList<Microsoft.Extensions.AI.ChatMessage> messages =
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
    messages.Add(new(ChatRole.User, "Please call the echo tool with the string 'Hello MCP!' and give me the response as-is."));

    // Call the chat client
    Console.WriteLine("Asking GPT-4o-mini to call the Echo Tool...");
    await foreach (var update in chatClient.GetStreamingResponseAsync(messages, new() { Tools = [.. mappedTools] }))
    {
        Console.Write(update);
    }
    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

async Task<IMcpClient> GetMcpClientAsync()
{
    McpClientOptions clientOptions = new()
    {
        ClientInfo = new() { Name = "SimpleToolsConsole", Version = "1.0.0" }
    };

    McpServerConfig serverConfig = new()
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

    return await McpClientFactory.CreateAsync(serverConfig, clientOptions);
}