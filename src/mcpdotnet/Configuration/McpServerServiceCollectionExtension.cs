using McpDotNet.Configuration;
using McpDotNet.Hosting;
using McpDotNet.Protocol.Transport;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

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
        services.AddHostedService<McpServerHostedService>();
        services.AddOptions();
        services.AddSingleton(services =>
        {
            IServerTransport serverTransport = services.GetRequiredService<IServerTransport>();
            McpServerOptions options = services.GetRequiredService<McpServerOptions>();
            ILoggerFactory? loggerFactory = services.GetService<ILoggerFactory>();

            if (services.GetService<IOptions<McpServerHandlers>>() is { } handlersOptions)
            {
                options = handlersOptions.Value.OverwriteWithSetHandlers(options);
            }

            return McpServerFactory.Create(serverTransport, options, loggerFactory, services);
        });

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
