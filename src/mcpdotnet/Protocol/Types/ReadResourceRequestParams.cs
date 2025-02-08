﻿namespace McpDotNet.Protocol.Types;

/// <summary>
/// Sent from the client to the server, to read a specific resource URI.
/// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/2024-11-05/schema.json">See the schema for details</see>
/// </summary>
public class ReadResourceRequestParams
{
    /// <summary>
    /// The URI of the resource to read. The URI can use any protocol; it is up to the server how to interpret it.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("uri")]
    public string? Uri { get; init; }
}
