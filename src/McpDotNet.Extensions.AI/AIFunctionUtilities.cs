using McpDotNet.Protocol.Types;
using System.Text.Json;

namespace McpDotNet.Extensions.AI;

internal static class AIFunctionUtilities
{
    /// <summary>
    /// Maps a Tool to a JsonElement that represents the tool's schema (top-level and input schema/parameters).
    /// </summary>
    public static JsonElement MapToJsonElement(Tool tool)
    {
        // Create a JsonObject that matches the MEAI schema format
        var schemaObj = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["title"] = tool.Name,
            ["description"] = tool.Description ?? string.Empty,
            ["properties"] = tool.InputSchema?.Properties ?? new(),
            ["required"] = tool.InputSchema?.Required ?? []
        };

        // Convert to JsonElement
        return JsonSerializer.SerializeToElement(schemaObj);
    }
}
