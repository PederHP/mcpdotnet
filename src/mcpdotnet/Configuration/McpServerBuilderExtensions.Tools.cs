using System.Reflection;
using System.Text.Json;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Types;
using McpDotNet.Server;
using Microsoft.Extensions.DependencyInjection;

namespace McpDotNet;

/// <summary>
/// Extension to configure the MCP server with tools
/// </summary>
public static partial class McpServerBuilderExtensions
{
    /// <summary>
    /// Adds a tool to the server.
    /// </summary>
    /// <typeparam name="TTool">The tool type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithTool<TTool>(this IMcpServerBuilder builder)
    {
        return WithTools(builder, typeof(TTool));
    }
    /// <summary>
    /// Adds all tools marked with <see cref="McpToolTypeAttribute"/> from the current assembly to the server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    public static IMcpServerBuilder WithTools(this IMcpServerBuilder builder)
    {
        return WithToolsFromAssembly(builder, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Adds tools to the server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="toolTypes">Types with marked methods to add as tools to the server.</param>
    public static IMcpServerBuilder WithTools(this IMcpServerBuilder builder, params Type[] toolTypes)
    {
        ArgumentNullException.ThrowIfNull(toolTypes);
        if (toolTypes.Length == 0)
            throw new ArgumentException("At least one tool type must be provided.", nameof(toolTypes));

        var tools = new List<Tool>();
        Dictionary<string, Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>>> callbacks = [];

        foreach (var type in toolTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<McpToolAttribute>();
                if (attribute != null)
                {
                    var tool = CreateTool(method, attribute);
                    tools.Add(tool);

                    callbacks.Add(tool.Name, async (request, cancellationToken) => await CallTool(request, method, cancellationToken));

                    // register type because method is not static and so we need an instance
                    if (!method.IsStatic)
                        builder.Services.AddScoped(type);
                }
            }
        }

        builder.WithListToolsHandler((_, _) => Task.FromResult(new ListToolsResult() { Tools = tools }));

        builder.WithCallToolHandler(async (request, cancellationToken) =>
        {
            if (request.Params != null && callbacks.TryGetValue(request.Params.Name, out var callback))
                return await callback(request, cancellationToken);

            throw new McpServerException($"Unknown tool: {request.Params?.Name}");
        });

        return builder;
    }

    /// <summary>
    /// Adds types marked with the <see cref="McpToolTypeAttribute"/> attribute from the given assembly as tools to the server.
    /// </summary>
    /// <param name="builder">The builder instance.</param>
    /// <param name="assembly">The assembly to load the types from. Null to get the current assembly</param>
    public static IMcpServerBuilder WithToolsFromAssembly(this IMcpServerBuilder builder, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        List<Type> toolTypes = [];

        foreach (var type in assembly.GetTypes())
        {
            bool hasToolTypeAttribute = type.GetCustomAttribute<McpToolTypeAttribute>() != null;
            if (!hasToolTypeAttribute)
                continue;

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<McpToolAttribute>();
                if (attribute != null)
                {
                    toolTypes.Add(type);
                    break;
                }
            }
        }

        if (toolTypes.Count == 0)
            throw new ArgumentException("No types with marked methods found in the assembly.", nameof(assembly));

        return WithTools(builder, toolTypes.ToArray());
    }

    private static Tool CreateTool(MethodInfo method, McpToolAttribute attribute)
    {
        Dictionary<string, JsonSchemaProperty> properties = [];
        List<string>? requiredProperties = null;

        foreach (var parameter in method.GetParameters())
        {
            if (parameter.ParameterType == typeof(CancellationToken))
                continue;

            var parameterAttribute = parameter.GetCustomAttribute<McpParameterAttribute>();

            properties.Add(parameter.Name ?? "NoName", new JsonSchemaProperty()
            {
                Type = GetParameterType(parameter.ParameterType),
                Description = parameterAttribute?.Description
            });

            if (parameterAttribute?.Required == true)
            {
                requiredProperties ??= [];
                requiredProperties.Add(parameter.Name ?? "NoName");
            }
        }

        return new Tool()
        {
            Name = attribute.Name ?? method.Name,
            Description = attribute.Description,
            InputSchema = new JsonSchema()
            {
                Type = "object",
                Properties = properties,
                Required = requiredProperties
            },
        };
    }

    private static string GetParameterType(Type parameterType)
    {
        return parameterType switch
        {
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) || t == typeof(double) || t == typeof(float) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t.IsArray => "array",
            Type t when t == typeof(DateTime) => "string",
            _ => "object"
        };
    }

    private static async Task<CallToolResponse> CallTool(RequestContext<CallToolRequestParams> request, MethodInfo method, CancellationToken cancellationToken)
    {
        var methodParameters = method.GetParameters();
        List<object?> parameters = ResolveParameters(request, methodParameters, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return new CallToolResponse { Content = [new Content { Text = "Operation was cancelled" }] };

        try
        {
            using var scope = request.Server.ServiceProvider?.CreateScope();
            var objectInstance = CreateObjectInstance(method, scope?.ServiceProvider);
            var result = method.Invoke(objectInstance, parameters.ToArray());


            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }

            if (result is string resultString)
                return new CallToolResponse { Content = [new Content() { Text = resultString, Type = "text" }] };

            if (result is string[] resultStringArray)
                return new CallToolResponse { Content = resultStringArray.Select(s => new Content() { Text = s, Type = "text" }).ToList() };

            if (result is null)
                return new CallToolResponse { Content = [new Content() { Text = "null" }] };

            if (result is JsonElement jsonElement)
                return new CallToolResponse { Content = [new Content() { Text = jsonElement.GetRawText(), Type = "text" }] };

            return new CallToolResponse { Content = [new Content() { Text = result.ToString(), Type = "text" }] };
        }
        catch (TargetInvocationException e)
        {
            throw new McpServerException(e.Message, e);
        }
    }

    private static List<object?> ResolveParameters(RequestContext<CallToolRequestParams> request, ParameterInfo[] methodParameters, CancellationToken cancellationToken)
    {
        var parameters = new List<object?>(methodParameters.Length);

        foreach (var parameter in methodParameters)
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parameters.Add(cancellationToken);
            }
            else if (request.Params?.Arguments != null && request.Params.Arguments.TryGetValue(parameter.Name ?? "NoName", out var value))
            {
                if (value is JsonElement element)
                    value = JsonSerializer.Deserialize(element.GetRawText(), parameter.ParameterType);

                parameters.Add(Convert.ChangeType(value, parameter.ParameterType));
            }
            else
            {
                var parameterAttribute = parameter.GetCustomAttribute<McpParameterAttribute>();

                if (parameterAttribute?.Required == true)
                    throw new McpServerException($"Missing required argument '{parameter.Name}'.");

                parameters.Add(parameter.HasDefaultValue ? parameter.DefaultValue : null);
            }
        }

        return parameters;
    }

    private static object? CreateObjectInstance(MethodInfo method, IServiceProvider? serviceProvider)
    {
        if (method.IsStatic)
            return null;

        if (serviceProvider != null)
            return ActivatorUtilities.CreateInstance(serviceProvider, method.DeclaringType!);

        return Activator.CreateInstance(method.DeclaringType!);
    }
}
