namespace McpDotNet.Protocol.Types;

/// <summary>
/// A request to include context from one or more MCP servers (including the caller), to be attached to the prompt.
/// </summary>
public enum ContextInclusion
{
    None,
    ThisServer,
    AllServers
}
