# mcpdotnet
[![NuGet version](https://img.shields.io/nuget/v/mcpdotnet.svg)](https://www.nuget.org/packages/mcpdotnet/)

A .NET client implementation of the Model Context Protocol (MCP), enabling .NET applications to connect to and interact with MCP servers.

## About MCP

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

For more information about MCP:
- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

## Design Goals

This library aims to provide a clean, specification-compliant implementation of the MCP protocol, with minimal additional abstraction. While transport implementations necessarily include additional code, they follow patterns established by the official SDKs where possible.

## Features

- MCP client implementation for .NET applications
- Support for stdio and SSE transports
- Support for all MCP capabilities: Tool, Resource, Prompt, Sampling, Roots
- Support for pagination and notifications
- Async/await pattern throughout
- Comprehensive logging support
- Compatible with .NET 8.0 and later

## Getting Started

To use mcpdotnet, first install it via NuGet:

```powershell
dotnet add package mcpdotnet
```

Then create a client and start using tools, or other capabilities, from the servers you configure:
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
            options,
            NullLoggerFactory.Instance
        );

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

It is also highly recommended that you pass a proper LoggerFactory instance to the factory constructor, to enable logging of MCP client operations.

Additional examples and documentation will be added as in the near future.

## Roadmap

- Expand documentation with detailed guides for:
  - Advanced scenarios (Sampling, Resources, Prompts)
  - Transport configuration
  - Error handling and recovery
- Increase test coverage
- Add example applications
- Performance optimization and profiling

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.