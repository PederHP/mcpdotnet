﻿namespace McpDotNet.Protocol.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// An error response message in the JSON-RPC protocol.
/// </summary>
internal record JsonRpcError : IJsonRpcMessageWithId
{
    /// <summary>
    /// JSON-RPC protocol version. Always "2.0".
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    /// <summary>
    /// Request identifier matching the original request.
    /// </summary>
    [JsonPropertyName("id")]
    public required RequestId Id { get; init; }

    /// <summary>
    /// Error information.
    /// </summary>
    [JsonPropertyName("error")]
    public required JsonRpcErrorDetail Error { get; init; }
}