﻿using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpDotNet.Utils.Json;
/// <summary>
/// Extensions for configuring System.Text.Json serialization options for MCP.
/// </summary>
internal static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Configures JsonSerializerOptions with MCP-specific settings and converters.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="loggerFactory">The logger factory to use for logging.</param>
    /// <returns>The configured options.</returns>
    public static JsonSerializerOptions ConfigureForMcp(this JsonSerializerOptions options, ILoggerFactory loggerFactory)
    {
        // Add custom converters
        options.Converters.Add(new JsonRpcMessageConverter(loggerFactory.CreateLogger<JsonRpcMessageConverter>()));
        options.Converters.Add(new JsonStringEnumConverter());

        // Configure general options
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.NumberHandling = JsonNumberHandling.AllowReadingFromString;

        return options;
    }
}
