using McpDotNet.Configuration;
using McpDotNet.Hosting;
using McpDotNet.Protocol.Transport;
using McpDotNet.Utils;
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
        Throw.IfNull(builder);

        builder.Services.AddSingleton<IServerTransport, StdioServerTransport>();
        builder.Services.AddHostedService<McpServerHostedService>();
        return builder;
    }

    /// <summary>
    /// Adds a server transport that uses SSE via a HttpListener for communication.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithHttpListenerSseServerTransport(this IMcpServerBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IServerTransport, HttpListenerSseServerTransport>();
        builder.Services.AddHostedService<McpServerHostedService>();
        return builder;
    }
}
