using McpDotNet.Client;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.AI;

namespace McpDotNet.Extensions.AI;

public static class McpToolExtensions
{
    public static AITool ToAITool(this Tool tool, IMcpClient client)
        => new McpAIFunction(tool, client);
}