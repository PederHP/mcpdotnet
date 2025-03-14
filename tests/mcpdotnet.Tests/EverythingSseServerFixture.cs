using System.Diagnostics;

namespace McpDotNet.Tests;

public class EverythingSseServerFixture : IAsyncDisposable
{
    private int _port;
    private string _containerName;

    public EverythingSseServerFixture(int port)
    {
        _port = port;
        _containerName = $"mcp-everything-server-{_port}";
    }

    public async Task StartAsync()
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run -p {_port}:3001 --name {_containerName} --rm tzolov/mcp-everything-server:v1",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        _ = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException($"Could not start process for {processStartInfo.FileName} with '{processStartInfo.Arguments}'.");

        // Wait for the server to start
        await Task.Delay(10000);
    }
    public async ValueTask DisposeAsync()
    {
        try
        {

            // Stop the container
            var stopInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"stop {_containerName}",
                UseShellExecute = false
            };

            using var stopProcess = Process.Start(stopInfo)
                ?? throw new InvalidOperationException($"Could not stop process for {stopInfo.FileName} with '{stopInfo.Arguments}'.");
            await stopProcess.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            // Log the exception but don't throw
            await Console.Error.WriteLineAsync($"Error stopping Docker container: {ex.Message}");
        }

        GC.SuppressFinalize(this);
    }
}