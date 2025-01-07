# mcpdotnet
[![NuGet version](https://img.shields.io/nuget/v/mcpdotnet.svg?style=flat)](https://www.nuget.org/packages/mcpdotnet/)

A .NET client implementation of the Model Context Protocol (MCP), enabling .NET applications to connect to and interact with MCP servers.

## About MCP

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

For more information about MCP:
- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

## Status

This project is in early development. While functional, it may undergo significant changes as the protocol and implementation evolve.

### Current Limitations
- Only supports the Tools, Resources, Prompts, Sampling capabilities (Roots not yet implemented)
- Only stdio transport is currently supported (HTTP/SSE transport coming soon)
- Client library only - server implementation support may be added in the future
- Provider-agnostic implementation focused purely on the MCP protocol
- Applications using this library need to implement their own integration with LLM APIs/clients

The library's current focus is on implementing the core MCP protocol, with the main utility being the ability to launch and connect to MCP servers based on configuration objects.

## Features

- MCP client implementation for .NET applications
- Support for stdio transport
- Support for Tool, Resource, Prompt, Sampling capabilities
- Support for pagination and notifications
- Async/await pattern throughout
- Compatible with .NET 8.0 and later

## Getting Started

To create a client you need to instantiate a factory with configuration objects:
```csharp
var options = new McpClientOptions() 
    { ClientInfo = new() { Name = "TestClient", Version = "1.0.0" } };
	
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
            options
        );
```

You can then retrieve an IMcpClient reference using the `id` of a server from the configuration, and use `ListToolsAsync` and `CallToolAsync` to integrate with your LLM-based code.

```csharp
var client = await factory.GetClientAsync("everything");
// Get the list of tools, for passing to an LLM
var tools = await client.ListToolsAsync();
// Execute a tool, in practice this would normally be driven by LLM tool invocations
var result = await client.CallToolAsync(
            "echo",
            new Dictionary<string, object>
            {
                ["message"] = "Hello MCP!"
            },
            CancellationToken.None
        );
// echo always returns one and only one text content object
Console.WriteLine(result.Content.FirstOrDefault(c => c.Type == "text").Text);
```

Note that you should pass CancellationToken objects suitable for your use case, to enable proper error handling, timeouts, etc. This example also does not paginate the tools list, which may be necessary for large tool sets. See the IntegrationTests project for an example of pagination, as well as examples of how to handle Prompts and Resources.

Additional examples and documentation will be added as in the near future.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.