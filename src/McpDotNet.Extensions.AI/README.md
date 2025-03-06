# McpDotNet.Extensions.AI
[![NuGet version](https://img.shields.io/nuget/v/McpDotNet.Extensions.AI.svg)](https://www.nuget.org/packages/McpDotNet.Extensions.AI/)

Microsoft.Extensions.AI integration for the Model Context Protocol (MCP). Enables seamless use of MCP tools as AI functions in any IChatClient implementation. Built on top of [mcpdotnet](https://github.com/PederHP/mcpdotnet).

## About MCP

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

For more information about MCP:
- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

## Design Goals

The goal of this library is to provide a simple and efficient way to integrate MCP capabilities with Microsoft.Extensions.AI.Abstractions. It is designed to be easy to use, wrapping boilerplate code and providing a clean API for AI developers to use leverage MCP capabilities without having to worry about the underlying protocol.

## Features

- Easy lifecycle management of MCP connections
- Seamless mapping of MCP tools to Microsoft.Extensions.AI.Abstractions AIFunction objects
- Use convenience methods, while still having access to the full MCP protocol through the underlying mcpdotnet library
- Compatible with .NET 8.0 and later

## Getting Started (Client)

To use McpDotNet.Extensions.AI, first install it via NuGet:

```powershell
dotnet add package McpDotNet.Extensions.AI
```

Create a configuration object for the MCP server you want to connect to:
```csharp
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
```

Then create an MCP session scope and use it with a chat client of your choice:
```csharp
await using var sessionScope = await McpSessionScope.CreateAsync(config);

IChatClient openaiClient = new OpenAIClient("sk-your-key")
    .AsChatClient("gpt-4o-mini");

IChatClient chatClient = new ChatClientBuilder(openaiClient)
    .UseFunctionInvocation()
    .Build();

// Create message list
IList<ChatMessage> messages =
[
    // Add a system message
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "Placeholder for the actual user message.")
];

// Call the chat client
var response = await chatClient.GetResponseAsync(messages,
        new() { Tools = sessionScope.Tools });
```

As you can see, all you really need to do is create one or more configuration objects matching the MCP servers you want to use, create an MCP session scope, and then the tools exposed by the server(s) will be available to you through the `Tools` property of the `McpSessionScope` object.

The `McpSessionScope` object implements `IAsyncDisposable`, and managing the connection lifecycle is as simple as using the `await using` pattern. Note that you have to keep the `McpSessionScope` object alive for as long as you want to use the tools it provides.

## Roadmap

- Convenience methods for other capabilities: prompts, sampling, resources, etc.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
