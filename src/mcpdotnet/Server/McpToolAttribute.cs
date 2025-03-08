namespace McpDotNet.Server;

/// <summary>
/// Attribute to mark a method as an MCP tool.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpToolAttribute : Attribute
{
    /// The name of the tool. If not provided, the class name will be used.  
    public string? Name { get; }

    /// <summary>
    /// A description of the tool.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Attribute to mark a method as an MCP tool.
    /// </summary>
    /// <param name="name">The name of the tool. If not provided, the class name will be used.</param>
    /// <param name="description">A description of the tool.</param>
    public McpToolAttribute(string? name = null, string? description = null)
    {
        Name = name;
        Description = description;
    }
}
