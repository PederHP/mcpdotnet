﻿using McpDotNet.Protocol.Messages;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// The server's response to a resources/list request from the client.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/schema.json">See the schema for details</see>
/// </summary>
public class ListResourcesResult : PaginatedResult
{
    /// <summary>
    /// A list of resources that the server offers.
    /// </summary>
    public List<Resource> Resources { get; set; } = new();
}