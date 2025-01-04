namespace McpDotNet.Protocol.Types;

/// <summary>
/// A prompt or prompt template that the server offers.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class Prompt
{
    /// <summary>
    /// A list of arguments to use for templating the prompt.
    /// </summary>
    public List<PromptArgument>? Arguments { get; set; }

    /// <summary>
    /// An optional description of what this prompt provides
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The name of the prompt or prompt template.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
