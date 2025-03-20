﻿using Anthropic.SDK.Common;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpDotNet;

public static class McpToolExtensions
{
    public static IList<Anthropic.SDK.Common.Tool> ToAnthropicTools(this IEnumerable<McpDotNet.Protocol.Types.McpTool> tools)
    {
        if (tools is null)
        {
            throw new ArgumentNullException(nameof(tools));
        }

        List<Anthropic.SDK.Common.Tool> result = [];
        foreach (var tool in tools)
        {
            var function = tool.JsonSchema.GetPropertyCount() == 0
                ? new Function(tool.Name, tool.Description)
                : new Function(tool.Name, tool.Description, JsonSerializer.Serialize(tool.JsonSchema));
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