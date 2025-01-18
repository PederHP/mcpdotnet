﻿using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// Describes a message issued to or received from an LLM API.
/// </summary>
public class SamplingMessage
{
    /// <summary>
    /// Text or image content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public required Content Content { get; init; }

    /// <summary>
    /// The role of the message ("user" or "assistant").
    /// </summary>
    [JsonPropertyName("role")]
    public required Role Role { get; init; }  // "user" or "assistant"
}
