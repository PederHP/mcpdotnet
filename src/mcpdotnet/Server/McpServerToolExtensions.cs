using System.Reflection;
using System.Text.Json;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.DependencyInjection;

namespace McpDotNet.Server;

/// <summary>
/// Extension methods for <see cref="IMcpServer"/>.
/// </summary>
public static class McpServerToolExtensions
{
    /// <summary>
    /// Adds types marked with the <see cref="McpToolAttribute"/> attribute from the given assembly as tools to the server.
    /// </summary>
    /// <param name="server">The server instance.</param>
    /// <param name="assembly">The assembly to load the types from. Null to get the current assembly</param>
    public static void AddToolsFromAssembly(this IMcpServer server, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        List<Type> toolTypes = [];

        foreach (var type in assembly.GetTypes())
        {
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

        AddTools(server, toolTypes.ToArray());
    }

    /// <summary>
    /// Adds the methods marked with the <see cref="McpToolAttribute"/> attribute as tools to the server.
    /// </summary>
    /// <param name="server">The server instance.</param>
    /// <param name="toolTypes">Types with marked methods to add as tools to the server.</param>
    public static void AddTools(this IMcpServer server, params Type[] toolTypes)
    {
        ArgumentNullException.ThrowIfNull(toolTypes);
        if (toolTypes.Length == 0)
            throw new ArgumentException("At least one tool type must be provided.", nameof(toolTypes));

        var tools = new List<Tool>();
        Dictionary<string, Func<CallToolRequestParams, IServiceProvider?, CancellationToken, Task<CallToolResponse>>> callbacks = [];

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

                    callbacks.Add(tool.Name, async (request, serviceProvider, cancellationToken) => await CallTool(request, serviceProvider, method, cancellationToken));
                }
            }
        }

        server.ListToolsHandler = (request, cancellationToken) =>
        {
            return Task.FromResult(new ListToolsResult()
            {
                Tools = tools
            });
        };

        server.CallToolHandler = async (request, cancellationToken) =>
        {
            if (callbacks.TryGetValue(request.Name, out var callback))
                return await callback(request, server.ServiceProvider, cancellationToken);

            throw new McpServerException($"Unknown tool: {request.Name}");
        };
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

    private static async Task<CallToolResponse> CallTool(CallToolRequestParams requestParams, IServiceProvider? serviceProvider, MethodInfo method, CancellationToken cancellationToken)
    {
        var methodParameters = method.GetParameters();
        var parameters = new List<object?>(methodParameters.Length);

        foreach (var parameter in methodParameters)
        {
            if (parameter.ParameterType == typeof(CancellationToken))
            {
                parameters.Add(cancellationToken);
            }
            else if (requestParams.Arguments != null && requestParams.Arguments.TryGetValue(parameter.Name ?? "NoName", out var value))
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

        if (cancellationToken.IsCancellationRequested)
            return new CallToolResponse { Content = [new Content { Text = "Operation was cancelled" }] };

        try
        {
            using var scope = serviceProvider?.CreateScope();
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

    private static object? CreateObjectInstance(MethodInfo method, IServiceProvider? serviceProvider)
    {
        if (method.IsStatic)
            return null;

        if (serviceProvider != null)
            return ActivatorUtilities.CreateInstance(serviceProvider, method.DeclaringType!);

        return Activator.CreateInstance(method.DeclaringType!);
    }
}
