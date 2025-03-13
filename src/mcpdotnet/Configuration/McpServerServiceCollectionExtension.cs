using McpDotNet.Configuration;
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

        services.AddOptions();
        services.AddTransient<IConfigureOptions<McpServerOptions>, McpServerOptionsSetup>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return new DefaultMcpServerBuilder(services);
    }
}
