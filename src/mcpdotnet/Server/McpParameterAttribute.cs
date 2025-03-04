namespace McpDotNet.Server;

/// <summary>
/// Attribute to mark a method parameter
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class McpParameterAttribute : Attribute
{
    /// <summary>
    /// Defines if the parameter is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// A description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Attribute to mark a method as an MCP tool.
    /// </summary>
    /// <param name="required">True if the parameter is mandatory.</param>
    /// <param name="description">A description of the tool.</param>
    public McpParameterAttribute(bool required = false, string? description = null)
    {
        Required = required;
        Description = description;
    }
}
