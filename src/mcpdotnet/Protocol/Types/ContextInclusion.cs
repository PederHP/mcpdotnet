namespace McpDotNet.Protocol.Types;

/// <summary>
/// A request to include context from one or more MCP servers (including the caller), to be attached to the prompt.
/// </summary>
public enum ContextInclusion
{
    /// <summary>
    /// No context should be included.
    /// </summary>
    None,

    /// <summary>
    /// Include context from the server that sent the request.
    /// </summary>
    ThisServer,

    /// <summary>
    /// Include context from all servers that the client is connected to.
    /// </summary>
    AllServers
}
