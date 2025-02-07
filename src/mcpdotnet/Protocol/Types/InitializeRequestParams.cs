﻿namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Parameters for an initialization request sent to the server.
/// </summary>
public record InitializeRequestParams
{
    /// <summary>
    /// The version of the Model Context Protocol that the client wants to use.
    /// </summary>
    [JsonPropertyName("protocolVersion")]

    public required string ProtocolVersion { get; init; }
    /// <summary>
    /// The client's capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ClientCapabilities? Capabilities { get; init; }

    /// <summary>
    /// Information about the client implementation.
    /// </summary>
    [JsonPropertyName("clientInfo")]
    public required Implementation ClientInfo { get; init; }
}
