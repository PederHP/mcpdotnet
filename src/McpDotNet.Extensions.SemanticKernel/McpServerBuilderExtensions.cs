using System.Text.Json;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.SemanticKernel;

namespace McpDotNet.Extensions.SemanticKernel;

/// <summary>
/// Extension methods to integrate Semantic Kernel with MCP server
/// </summary>
public static class McpServerBuilderSemanticKernelExtensions
{
    /// <summary>
    /// Adds a Semantic Kernel instance to the server, exposing all its registered plugins as MCP tools.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="kernel">The Semantic Kernel instance containing the plugins to expose.</param>
    /// <returns>The builder instance.</returns>
    public static IMcpServerBuilder WithSemanticKernel(this IMcpServerBuilder builder, Kernel kernel)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        if (kernel is null)
            throw new ArgumentNullException(nameof(kernel));

        var tools = new List<Tool>();
        Dictionary<string, Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>>> callbacks = [];

        // Process all plugins and their functions
        foreach (var plugin in kernel.Plugins)
        {
            var pluginName = plugin.Name;
            
            // Iterate through functions using the IEnumerable<KernelFunction> implementation
            foreach (var function in plugin)
            {
                var functionName = function.Name;

                // Get description from the function metadata
                var description = function.Metadata.Description;
                
                // Create tool schema from the function's metadata
                var tool = CreateToolFromKernelFunction(function.Metadata, pluginName);
                tools.Add(tool);

                // Register callback to invoke the function via kernel
                callbacks.Add(tool.Name, async (request, cancellationToken) => 
                    await CallKernelFunction(kernel, pluginName, functionName, request, cancellationToken).ConfigureAwait(false));
            }
        }

        if (tools.Count == 0)
            throw new ArgumentException("No functions found in the provided Kernel.", nameof(kernel));

        builder.WithListToolsHandler((_, _) => Task.FromResult(new ListToolsResult() { Tools = tools }));

        builder.WithCallToolHandler(async (request, cancellationToken) =>
        {
            if (request.Params != null && callbacks.TryGetValue(request.Params.Name, out var callback))
                return await callback(request, cancellationToken).ConfigureAwait(false);

            throw new McpServerException($"Unknown tool: {request.Params?.Name}");
        });

        return builder;
    }
    
    /// <summary>
    /// Creates a Tool object from a Semantic Kernel function
    /// </summary>
    private static Tool CreateToolFromKernelFunction(KernelFunctionMetadata function, string pluginName)
    {
        var parameters = function.Parameters;
        Dictionary<string, JsonSchemaProperty> properties = [];
        List<string>? requiredProperties = null;

        foreach (var parameter in parameters)
        {
            properties.Add(parameter.Name, new JsonSchemaProperty()
            {
                Type = GetParameterType(parameter.ParameterType!),
                Description = parameter.Description,
            });

            if (parameter.IsRequired)
            {
                requiredProperties ??= [];
                requiredProperties.Add(parameter.Name);
            }
        }

        return new Tool()
        {
            Name = string.IsNullOrEmpty(pluginName) 
                ? function.Name 
                : $"{pluginName}-{function.Name}",
            Description = function.Description,
            InputSchema = new JsonSchema()
            {
                Type = "object",
                Properties = properties,
                Required = requiredProperties
            }
        };
    }

    /// <summary>
    /// Calls a Semantic Kernel function through the kernel instance
    /// </summary>
    private static async Task<CallToolResponse> CallKernelFunction(
        Kernel kernel, 
        string pluginName, 
        string functionName, 
        RequestContext<CallToolRequestParams> request, 
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create arguments from the request
        var arguments = new KernelArguments();

        // Add all arguments from the request
        if (request.Params?.Arguments != null)
        {
            foreach (var arg in request.Params.Arguments)
            {
                if (arg.Value is JsonElement element)
                {
                    // Handle JsonElement based on its type
                    switch (element.ValueKind)
                    {
                        case JsonValueKind.String:
                            arguments[arg.Key] = element.GetString();
                            break;
                        case JsonValueKind.Number:
                            if (element.TryGetInt32(out var intValue))
                                arguments[arg.Key] = intValue;
                            else if (element.TryGetDouble(out var doubleValue))
                                arguments[arg.Key] = doubleValue;
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            arguments[arg.Key] = element.GetBoolean();
                            break;
                        case JsonValueKind.Array:
                        case JsonValueKind.Object:
                            // Use the JsonElement directly and let Semantic Kernel handle deserialization
                            arguments[arg.Key] = element;
                            break;
                        case JsonValueKind.Null:
                            arguments[arg.Key] = null;
                            break;
                    }
                }
                else
                {
                    arguments[arg.Key] = arg.Value;
                }
            }
        }

        try
        {
            // Invoke the function using the kernel
            var result = await kernel.InvokeAsync(pluginName, functionName, arguments, cancellationToken).ConfigureAwait(false);
            
            // Process the result
            return ProcessFunctionResult(result.GetValue<object>());
        }
        catch (Exception ex)
        {
            // Handle any errors during function execution
            return new CallToolResponse 
            { 
                Content = [new Content() { Text = $"Error executing function: {ex.Message}", Type = "text" }] 
            };
        }
    }

    /// <summary>
    /// Processes the result of a function execution and converts it to a CallToolResponse
    /// </summary>
    private static CallToolResponse ProcessFunctionResult(object? result)
    {
        if (result is string resultString)
            return new CallToolResponse { Content = [new Content() { Text = resultString, Type = "text" }] };

        if (result is string[] resultStringArray)
            return new CallToolResponse { Content = [.. resultStringArray.Select(s => new Content() { Text = s, Type = "text" })] };

        if (result is null)
            return new CallToolResponse { Content = [new Content() { Text = "null", Type = "text" }] };

        if (result is JsonElement jsonElement)
            return new CallToolResponse { Content = [new Content() { Text = jsonElement.GetRawText(), Type = "text" }] };

        // For complex objects, serialize to JSON
        var serialized = JsonSerializer.Serialize(result);
        return new CallToolResponse { Content = [new Content() { Text = serialized, Type = "text" }] };
    }
    
    private static string GetParameterType(Type parameterType)
    {
        return parameterType switch
        {
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) || t == typeof(double) || t == typeof(float) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t.IsArray => "array",
            Type t when t == typeof(DateTime) || t == typeof(DateTimeOffset) => "string",
            _ => "object"
        };
    }
} 