using System.Reflection;
using McpDotNet.Configuration;
using McpDotNet.Hosting;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;

namespace McpDotNet;

/// <summary>
/// Extension to host the MCP server
/// </summary>
public static class McpServerServiceCollectionExtension
{
    /// <summary>
    /// Adds the MCP server to the service collection with default options.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static IMcpServerBuilder AddMcpServer(this IServiceCollection services, Action<McpServerOptions>? configureOptions = null)
    {
        var options = CreateDefaultServerOptions();
        configureOptions?.Invoke(options);

        return AddMcpServer(services, options);
    }

    /// <summary>
    /// Adds the MCP server to the service collection with the provided options.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serverOptions"></param>
    /// <returns></returns>
    public static IMcpServerBuilder AddMcpServer(this IServiceCollection services, McpServerOptions serverOptions)
    {
        services.AddSingleton(serverOptions);
        services.AddSingleton<IMcpServerFactory, McpServerFactory>();
        services.AddHostedService<McpServerHostedService>();
        services.AddOptions();

        services.AddSingleton<IMcpServer>(sp => sp.GetRequiredService<IMcpServerFactory>().CreateServer());

        return new DefaultMcpServerBuilder(services);
    }

    private static McpServerOptions CreateDefaultServerOptions()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.GetName();

        return new McpServerOptions()
        {
            ServerInfo = new Implementation() { Name = assemblyName?.Name ?? "McpServer", Version = assemblyName?.Version?.ToString() ?? "1.0.0" },
            Capabilities = new ServerCapabilities()
            {
                Tools = new(),
                Resources = new(),
                Prompts = new(),
            },
            ProtocolVersion = "2024-11-05"
        };
    }
}
