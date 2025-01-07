// Protocol/Transport/StdioTransport.cs
namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Represents configuration options for the stdio transport.
/// </summary>
public record StdioTransportOptions
{
    /// <summary>
    /// The command to execute to start the server process.
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Arguments to pass to the server process.
    /// </summary>
    public string[]? Arguments { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The working directory for the server process.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Environment variables to set for the server process.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// The timeout to wait for the server to shut down gracefully.
    /// </summary>
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
