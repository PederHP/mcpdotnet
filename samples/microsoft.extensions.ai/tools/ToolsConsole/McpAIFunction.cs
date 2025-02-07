using McpDotNet.Client;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.AI;
using SimpleToolsConsole;

namespace SimpleToolsConsole;

public class McpAIFunction : AIFunction
{
    private readonly Tool _tool;
    private readonly IMcpClient _client;
    private readonly AIFunctionMetadata _metadata;

    public McpAIFunction(Tool tool, IMcpClient client)
    {
        _tool = tool ?? throw new ArgumentNullException(nameof(tool));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _metadata = new AIFunctionMetadata(tool.Name)
        {
            Description = tool.Description,
            Parameters = MapFunctionParameters(tool.InputSchema),
            ReturnParameter = new AIFunctionReturnParameterMetadata()
            {
                ParameterType = typeof(string),
            }
        };
    }

    public override string ToString() => _tool.Name;

    protected async override Task<object?> InvokeCoreAsync(IEnumerable<KeyValuePair<string, object?>> arguments, CancellationToken cancellationToken)
    {
        // Convert arguments to dictionary format expected by mcpdotnet
        Dictionary<string, object> argDict = new();
        foreach (var arg in arguments)
        {
            if (arg.Value is not null)
            {
                argDict[arg.Key] = arg.Value;
            }
        }

        // Call the tool through mcpdotnet
        var result = await _client.CallToolAsync(
            _tool.Name,
            argDict.Count == 0 ? new(): argDict,
            cancellationToken: cancellationToken
        );

        // Extract the text content from the result
        // For simplicity in this sample, we'll just concatenate all text content
        return string.Join("\n", result.Content
            .Where(c => c.Type == "text")
            .Select(c => c.Text));
    }

    public override AIFunctionMetadata Metadata => _metadata;

    private static List<AIFunctionParameterMetadata> MapFunctionParameters(JsonSchema? jsonSchema)
    {
        var properties = jsonSchema?.Properties;
        if (properties == null)
        {
            return new();
        }
        HashSet<string> requiredProperties = new(jsonSchema!.Required ?? []);
        return properties.Select(kvp =>
            new AIFunctionParameterMetadata(kvp.Key)
            {
                Description = kvp.Value.Description,
                ParameterType = McpTypeMapper.MapJsonToDotNetType(kvp.Value.Type, requiredProperties.Contains(kvp.Key)),
                IsRequired = requiredProperties.Contains(kvp.Key)
            }).ToList();
    }
}
