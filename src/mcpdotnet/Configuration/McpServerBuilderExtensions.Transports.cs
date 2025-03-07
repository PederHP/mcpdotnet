using McpDotNet.Configuration;
using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace McpDotNet;

/// <summary>
/// Extension to configure the MCP server with transports
/// </summary>
public static partial class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds a server transport that uses stdin/stdout for communication.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithStdioServerTransport(this IMcpServerBuilder builder)
    {
        builder.Services.AddSingleton<IServerTransport, StdioServerTransport>();
        return builder;
    }
}
