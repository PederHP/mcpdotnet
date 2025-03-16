using McpDotNet;
using McpDotNet.Extensions.SemanticKernel;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Serilog;
using Serilog.Events;
using SemanticKernelIntegration.Plugins;

namespace SemanticKernelIntegration;

/// <summary>
/// Sample application demonstrating Semantic Kernel integration with MCP
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Configure Serilog
        ConfigureSerilog();

        try
        {
            Log.Information("Starting Semantic Kernel Integration sample...");

            // 1. Set up the Semantic Kernel
            var kernel = ConfigureSemanticKernel();

            // 2. Create an application builder
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            // 3. Configure MCP server
            builder.Services
                .AddMcpServer()
                .WithSemanticKernel(kernel)
                .WithListPromptsHandler((_, _) => Task.FromResult(new ListPromptsResult()))
                .WithGetPromptHandler((_, _) => Task.FromResult(new GetPromptResult()))
                .WithListResourcesHandler((_, _) => Task.FromResult(new ListResourcesResult()))
                .WithReadResourceHandler((_, _) => Task.FromResult(new ReadResourceResult()))
                .WithStdioServerTransport();

            // 4. Add Serilog to the service collection
            builder.Services.AddSerilog();

            // 5. Build the host
            var host = builder.Build();

            // 6. Start the host
            await host.StartAsync();

            Log.Information("MCP server started successfully. Waiting for requests...");

            // Run until process is stopped by the client (parent process)
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "A critical error occurred during sample execution");
        }
        finally
        {
            // Ensure all logs are flushed before the application exits
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Configures Serilog with file, debug, and stderr sinks
    /// </summary>
    private static void ConfigureSerilog()
    {
        // Create logs directory if it doesn't exist
        var logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose() // Capture all log levels
            .WriteTo.File(
                Path.Combine(logsDirectory, "SemanticKernelIntegration_.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .WriteTo.Console(
                standardErrorFromLevel: LogEventLevel.Verbose, // Send all logs to stderr
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    /// <summary>
    /// Configures the Semantic Kernel with plugins
    /// </summary>
    private static Kernel ConfigureSemanticKernel()
    {
        var skBuilder = Kernel.CreateBuilder();

        // Set up Serilog for Semantic Kernel
        skBuilder.Services.AddSerilog();

        Log.Information("Registering plugins with Semantic Kernel...");

        // Register plugins with the kernel
        skBuilder.Plugins.AddFromType<TimePlugin>();
        skBuilder.Plugins.AddFromType<CalculatorPlugin>();
        skBuilder.Plugins.AddFromType<WeatherPlugin>();

        Log.Information("Plugins registered successfully");

        return skBuilder.Build();
    }
}