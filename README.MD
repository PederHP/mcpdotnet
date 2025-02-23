# mcpdotnet
[![NuGet version](https://img.shields.io/nuget/v/mcpdotnet.svg)](https://www.nuget.org/packages/mcpdotnet/)

A .NET implementation of the Model Context Protocol (MCP), enabling .NET applications to connect to and interact with MCP clients and servers.

## About MCP

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

For more information about MCP:
- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)

## Available Packages

| Package | Description | Documentation |
|---------|-------------|---------------|
| [mcpdotnet](src/mcpdotnet) | Core MCP implementation for .NET | [README](README.md) |
| [McpDotNet.Extensions.AI](src/McpDotNet.Extensions.AI) | Integration with Microsoft.Extensions.AI | [README](src/McpDotNet.Extensions.AI/README.md) |

## Design Goals

This library aims to provide a clean, specification-compliant implementation of the MCP protocol, with minimal additional abstraction. While transport implementations necessarily include additional code, they follow patterns established by the official SDKs where possible.

## Features

- MCP implementation for .NET applications
- Support for stdio and SSE transports (Clients)
- Support for stdio transport (Servers)
- Support for all MCP capabilities: Tool, Resource, Prompt, Sampling, Roots
- Support for the Completion utility capability
- Support for server instructions, pagination and notifications
- Async/await pattern throughout
- Comprehensive logging support
- Compatible with .NET 8.0 and later

## Getting Started (Client)

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

You can find samples demonstrating how to use mcpdotnet with an LLM SDK in the [samples](samples) directory, and also refer to the [IntegrationTests](test/McpDotNet.IntegrationTests) project for more examples.

Additional examples and documentation will be added as in the near future.

Remember you can connect to any MCP server, not just ones created using mcpdotnet. The protocol is designed to be server-agnostic, so you can use this library to connect to any compliant server.

## Getting Started (Server)

Here is an example of how to create an MCP server with a single tool. You can also refer to the TestServer in the tests folder, which implements a wider range of capabilities.

```csharp
McpServerOptions options = new McpServerOptions()
{
    ServerInfo = new Implementation() { Name = "MyServer", Version = "1.0.0" },
    Capabilities = new ServerCapabilities()
    {
        Tools = new()
    }
};
McpServerFactory factory = new McpServerFactory(new StdioServerTransport("MyServer", loggerFactory), options, loggerFactory);
IMcpServer server = factory.CreateServer();

server.ListToolsHandler = (request, cancellationToken) =>
{
    return Task.FromResult(new ListToolsResult()
    {
        Tools = 
        [
            new Tool()                
            {
                Name = "echo",
                Description = "Echoes the input back to the client.",
                InputSchema = new JsonSchema()
                {
                    Type = "object",
                    Properties = new Dictionary<string, JsonSchemaProperty>()
                    {
                        ["message"] = new JsonSchemaProperty() { Type = "string", Description = "The input to echo back." }
                    }
                },
            }
        ]
    });
};

server.CallToolHandler = async (request, cancellationToken) =>
{
    if (request.Name == "echo")
    {
        if (request.Arguments is null || !request.Arguments.TryGetValue("message", out var message))
        {
            throw new McpServerException("Missing required argument 'message'");
        }
        return new CallToolResponse()
        {
            Content = [new Content() { Text = "Echo: " + message.ToString(), Type = "text" }]
        };
    }
    else
    {
        throw new McpServerException($"Unknown tool: {request.Name}");
    }
};

await server.StartAsync();

// Run until process is stopped by the client (parent process)
while (true)
{
    await Task.Delay(1000);
}
```

## Roadmap

- Expand documentation with detailed guides for:
  - Advanced scenarios (Sampling, Resources, Prompts)
  - Transport configuration
  - Error handling and recovery
- Increase test coverage
- Add additional samples and examples
- Performance optimization
- SSE server support
- Authentication

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
