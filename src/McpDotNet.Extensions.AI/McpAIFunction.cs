using McpDotNet.Client;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace McpDotNet.Extensions.AI;

/// <summary>
/// Represents an AI function that calls a tool through mcpdotnet.
/// </summary>
public class McpAIFunction : AIFunction
{
    private readonly Tool _tool;
    private readonly IMcpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpAIFunction"/> class.
    /// </summary>
    public McpAIFunction(Tool tool, IMcpClient client)
    {
        _tool = tool ?? throw new ArgumentNullException(nameof(tool));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc/>
    public override string Name => _tool.Name;

    /// <inheritdoc/>
    public override string Description => _tool.Description ?? string.Empty;

    /// <inheritdoc/>
    public override JsonElement JsonSchema => AIFunctionUtilities.MapToJsonElement(_tool);

    /// <inheritdoc/>
    protected async override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken)
    {
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        // Convert arguments to dictionary format expected by mcpdotnet
        Dictionary<string, object> argDict = [];
        foreach (var arg in arguments)
        {
            if (arg.Value is not null)
            {
                argDict[arg.Key] = arg.Value;
            }
        }

        // Call the tool through mcpdotnet
        var result = await _client.CallToolAsync(_tool.Name, argDict, cancellationToken).ConfigureAwait(false);

        // Extract the text content from the result.
        return string.Join("\n", result.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text));
    }
}
