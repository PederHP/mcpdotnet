﻿using System.Text.Json.Serialization;

namespace McpDotNet.Protocol.Types;

/// <summary>
/// Used for completion requests to provide additional context for the completion options.
/// </summary>
public class Argument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}