using McpDotNet.Configuration;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<IMcpServerFactory, McpServerFactory>();
        services.AddSingleton<IMcpServer>(sp => sp.GetRequiredService<IMcpServerFactory>().CreateServer());

        services.AddOptions();
        services.AddTransient<IConfigureOptions<McpServerOptions>, McpServerOptionsSetup>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return new DefaultMcpServerBuilder(services);
    }
}
