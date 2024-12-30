﻿namespace McpDotNet.Protocol.Types;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the capabilities that a client may support.
/// </summary>
public record ClientCapabilities
{
    /// <summary>
    /// Experimental, non-standard capabilities that the client supports.
    /// </summary>
    [JsonPropertyName("experimental")]
    public Dictionary<string, object>? Experimental { get; init; }

    /// <summary>
    /// Present if the client supports listing roots.
    /// </summary>
    [JsonPropertyName("roots")]
    public RootsCapability? Roots { get; init; }

    /// <summary>
    /// Present if the client supports sampling from an LLM.
    /// </summary>
    [JsonPropertyName("sampling")]
    public SamplingCapability? Sampling { get; init; }
}

/// <summary>
/// Represents the capabilities that a server may support.
/// </summary>
public record ServerCapabilities
{
    /// <summary>
    /// Experimental, non-standard capabilities that the server supports.
    /// </summary>
    [JsonPropertyName("experimental")]
    public Dictionary<string, object>? Experimental { get; init; }

    /// <summary>
    /// Present if the server supports sending log messages to the client.
    /// </summary>
    [JsonPropertyName("logging")]
    public LoggingCapability? Logging { get; init; }

    /// <summary>
    /// Present if the server offers any prompt templates.
    /// </summary>
    [JsonPropertyName("prompts")]
    public PromptsCapability? Prompts { get; init; }

    /// <summary>
    /// Present if the server offers any resources to read.
    /// </summary>
    [JsonPropertyName("resources")]
    public ResourcesCapability? Resources { get; init; }

    /// <summary>
    /// Present if the server offers any tools to call.
    /// </summary>
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; init; }
}

/// <summary>
/// Represents the roots capability configuration.
/// </summary>
public record RootsCapability
{
    /// <summary>
    /// Whether the client supports notifications for changes to the roots list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

/// <summary>
/// Represents the sampling capability configuration.
/// </summary>
public record SamplingCapability
{
    // Currently empty in the spec, but may be extended in the future
}

/// <summary>
/// Represents the logging capability configuration.
/// </summary>
public record LoggingCapability
{
    // Currently empty in the spec, but may be extended in the future
}

/// <summary>
/// Represents the prompts capability configuration.
/// </summary>
public record PromptsCapability
{
    /// <summary>
    /// Whether this server supports notifications for changes to the prompt list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

/// <summary>
/// Represents the resources capability configuration.
/// </summary>
public record ResourcesCapability
{
    /// <summary>
    /// Whether this server supports subscribing to resource updates.
    /// </summary>
    [JsonPropertyName("subscribe")]
    public bool? Subscribe { get; init; }

    /// <summary>
    /// Whether this server supports notifications for changes to the resource list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

/// <summary>
/// Represents the tools capability configuration.
/// </summary>
public record ToolsCapability
{
    /// <summary>
    /// Whether this server supports notifications for changes to the tool list.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}