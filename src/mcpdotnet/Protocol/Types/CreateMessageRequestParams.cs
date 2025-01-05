﻿namespace McpDotNet.Protocol.Types;

/// <summary>
/// A request from the server to sample an LLM via the client. 
/// The client has full discretion over which model to select. 
/// The client should also inform the user before beginning sampling, to allow them to inspect the request (human in the loop) and decide whether to approve it.
/// 
/// While these align with the protocol specification,
/// clients have full discretion over model selection and should inform users before sampling.
/// </summary>
public class CreateMessageRequestParams
{
    /// <summary>
    /// A request to include context from one or more MCP servers (including the caller), to be attached to the prompt. The client MAY ignore this request.
    /// </summary>
    public ContextInclusion? IncludeContext { get; init; }

    /// <summary>
    /// The maximum number of tokens to sample, as requested by the server. The client MAY choose to sample fewer tokens than requested.
    /// </summary>
    public int MaxTokens { get; init; }

    /// <summary>
    /// Messages requested by the server to be included in the prompt.
    /// </summary>
    public required IReadOnlyList<SamplingMessage> Messages { get; init; }

    /// <summary>
    /// Optional metadata to pass through to the LLM provider. The format of this metadata is provider-specific.
    /// </summary>
    public object? Metadata { get; init; }

    /// <summary>
    /// The server's preferences for which model to select. The client MAY ignore these preferences.
    /// </summary>
    public ModelPreferences? ModelPreferences { get; init; }

    /// <summary>
    /// Optional stop sequences that the server wants to use for sampling.
    /// </summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>
    /// An optional system prompt the server wants to use for sampling. The client MAY modify or omit this prompt.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// The temperature to use for sampling, as requested by the server.
    /// </summary>
    public float? Temperature { get; init; }
}