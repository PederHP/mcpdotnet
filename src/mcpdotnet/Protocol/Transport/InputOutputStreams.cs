namespace McpDotNet.Protocol.Transport;

/// <summary>
/// Container for the input and output streams.
/// </summary>
public class InputOutputStreams
{
    /// <summary>
    /// Gets the default input and output streams.
    /// </summary>
    public static readonly InputOutputStreams Default = new()
    {
        Output = Console.Out,
        Input = Console.In
    };

    /// <summary>
    /// Gets or sets the output stream.
    /// </summary>
    public required TextWriter Output { get; init; }

    /// <summary>
    /// Gets or sets the input stream.
    /// </summary>
    public required TextReader Input { get; init; }
}
