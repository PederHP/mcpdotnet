﻿using McpDotNet.Configuration;
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
    /// 
    /// NB! Make sure to not register a logger factory that logs to stdout, as it will interfere with the communication.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithStdioServerTransport(this IMcpServerBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IServerTransport, StdioServerTransport>();
        return builder;
    }
}
