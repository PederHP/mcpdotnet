using System.Reflection;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.Options;

namespace McpDotNet;

internal class McpServerOptionsSetup : IConfigureOptions<McpServerOptions>
{
    public void Configure(McpServerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
        options.ServerInfo = new Implementation
        {
            Name = assemblyName?.Name ?? "McpServer",
            Version = assemblyName?.Version?.ToString() ?? "1.0.0",
        };
    }
}
