namespace McpDotNet.Protocol.Types;

/// <summary>
/// Describes an argument that a prompt can accept.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class PromptArgument
{
    /// <summary>
    /// The name of the argument.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable description of the argument.
    /// </summary>
    public string? Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this argument must be provided.
    /// </summary>
    public bool? Required { get; set; }
}