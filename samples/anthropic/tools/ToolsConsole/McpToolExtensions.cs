using Anthropic.SDK.Common;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class McpToolExtensions
{
    public static IList<Anthropic.SDK.Common.Tool> ToAnthropicTools(this IEnumerable<McpDotNet.Protocol.Types.Tool> tools)
    {
        List<Anthropic.SDK.Common.Tool> result = new();
        foreach (var tool in tools)
        {
            var function = tool.InputSchema == null
                ? new Function(tool.Name, tool.Description)
                : new Function(tool.Name, tool.Description, JsonSerializer.Serialize(tool.InputSchema));
            result.Add(function);
        }
        return result;
    }

    public static Dictionary<string, object>? ToMCPArguments(this JsonNode jsonNode)
    {
        if (jsonNode == null)
            return null;

        // Convert JsonNode to Dictionary<string, object>
        return jsonNode.AsObject()
            .ToDictionary(
                prop => prop.Key,
                prop => JsonSerializer.Deserialize<object>(prop.Value)!
            );
    }
}