using McpDotNet.Client;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.AI;

namespace McpDotNet.Extensions.AI;

/// <summary>
/// Utility class for converting an MCP Tool to an Microsoft.Extensions.AI.Abstractions AITool.
/// </summary>
public static class McpToolExtensions
{
    /// <summary>
    /// Converts an MCP Tool to an AITool.
    /// </summary>
    public static AITool ToAITool(this Tool tool, IMcpClient client)
        => new McpAIFunction(tool, client);
}