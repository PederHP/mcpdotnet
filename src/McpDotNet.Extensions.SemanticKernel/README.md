# McpDotNet.Extensions.SemanticKernel

This package provides integration between Microsoft Semantic Kernel and the MCP (Microsoft Copilot Protocol) server implementation in mcpdotnet.

## Features

- Expose Semantic Kernel plugins as MCP tools
- Seamlessly integrate AI capabilities from Semantic Kernel with MCP clients
- Automatic conversion between Semantic Kernel function parameters and MCP tool schemas

## Getting Started

First, install the package:

```bash
dotnet add package McpDotNet.Extensions.SemanticKernel
```

Then, in your application:

```csharp
using McpDotNet;
using McpDotNet.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;

// Create a Semantic Kernel instance
var builder = Kernel.CreateBuilder();
// Configure your kernel with plugins, AI services, etc.
builder.Plugins.AddFromType<YourPlugin>();
var kernel = builder.Build();

// Create an MCP server and integrate with Semantic Kernel
var hostBuilder = Host.CreateApplicationBuilder();
hostBuilder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithSemanticKernel(kernel);

// Run the server
await hostBuilder.Build().RunAsync();
```

## Example Usage

### Define a Semantic Kernel Plugin

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class TimePlugin
{
    [KernelFunction, Description("Get the current time")]
    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }

    [KernelFunction, Description("Get the current date")]
    public string GetCurrentDate()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }
}
```

### Expose the Plugin via MCP

```csharp
// Register the plugin with Semantic Kernel
var builder = Kernel.CreateBuilder();
builder.Plugins.AddFromType<TimePlugin>();
var kernel = builder.Build();

// Create an MCP server with the Semantic Kernel integration
var hostBuilder = Host.CreateApplicationBuilder();
hostBuilder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithSemanticKernel(kernel);

// Run the server
await hostBuilder.Build().RunAsync();
```

Now your Semantic Kernel functions are available as MCP tools that can be called by any MCP client.

## How It Works

The integration:

1. Scans all plugins registered in the Semantic Kernel instance
2. Converts each function to an MCP tool with appropriate schema
3. Handles parameter conversion between MCP tool calls and Semantic Kernel functions
4. Returns function results as MCP tool responses

## Advanced Configuration

You can customize the integration by implementing your own handlers:

```csharp
// Custom handling for specific tools
hostBuilder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithSemanticKernel(kernel)
    .WithCallToolHandler((request, cancellationToken) => 
    {
        // Custom handling for specific tools
        if (request.Params?.Name == "custom-tool")
        {
            // Custom implementation
            return new CallToolResponse { ... };
        }
        
        // Fall back to default handler
        return null;
    });
``` 