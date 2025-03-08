namespace McpDotNet.Server;

/// <summary>
/// Attribute to mark a type as container for MCP tools.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class McpToolTypeAttribute : Attribute;
