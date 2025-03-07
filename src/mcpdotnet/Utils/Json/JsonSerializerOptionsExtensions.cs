using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpDotNet.Utils.Json;

/// <summary>
/// Extensions for configuring System.Text.Json serialization options for MCP.
/// </summary>
internal static class JsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions DefaultOptions { get; } = CreateDefaultOptions();

    /// <summary>
    /// Creates default options to use for MCP-related serialization.
    /// </summary>
    /// <returns>The configured options.</returns>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        JsonSerializerOptions options = new();

        // Add custom converters
        options.Converters.Add(new JsonRpcMessageConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        // Configure general options
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        return options;
    }
}
