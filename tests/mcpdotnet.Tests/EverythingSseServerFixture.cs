using System.Diagnostics;

namespace McpDotNet.Tests;

public class EverythingSseServerFixture : IAsyncDisposable
{
    private Process? _process;

    public async Task StartAsync()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "run -p 3001:3001 --rm tzolov/mcp-everything-server:v1",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        _process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Could not start process for {processStartInfo.FileName} with '{processStartInfo.Arguments}'.");

        // Wait for the server to start
        await Task.Delay(10000);
    }
    public async ValueTask DisposeAsync()
    {
        try
        {
            // Find the container ID
            var psInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter ancestor=tzolov/mcp-everything-server:v1",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            using var psProcess = Process.Start(psInfo)
                 ?? throw new InvalidOperationException($"Could not start process for {psInfo.FileName} with '{psInfo.Arguments}'.");
            string containerId = await psProcess.StandardOutput.ReadToEndAsync();
            containerId = containerId.Trim();

            if (!string.IsNullOrEmpty(containerId))
            {
                // Stop the container
                var stopInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"stop {containerId}",
                    UseShellExecute = false
                };

                using var stopProcess = Process.Start(stopInfo)
                    ?? throw new InvalidOperationException($"Could not start process for {stopInfo.FileName} with '{stopInfo.Arguments}'.");
                await stopProcess.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw
            await Console.Error.WriteLineAsync($"Error stopping Docker container: {ex.Message}");
        }

        GC.SuppressFinalize(this);
    }
}