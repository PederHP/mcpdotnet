﻿namespace McpDotNet.Protocol.Messages;

using System.Text.Json.Serialization;

/// <summary>
/// Detailed error information for JSON-RPC error responses.
/// </summary>
internal record JsonRpcErrorDetail
{
    /// <summary>
    /// Integer error code.
    /// </summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    /// <summary>
    /// Short description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional additional error data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}