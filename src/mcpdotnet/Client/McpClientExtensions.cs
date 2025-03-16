using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Types;
using McpDotNet.Utils;
using System.Runtime.CompilerServices;

namespace McpDotNet.Client;

/// <summary>
/// Provides extensions for operating on MCP clients.
/// </summary>
public static class McpClientExtensions
{
    /// <summary>
    /// Sends a notification to the server with parameters.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="method">The notification method name.</param>
    /// <param name="parameters">The parameters to send with the notification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task SendNotificationAsync(this IMcpClient client, string method, object? parameters = null, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendMessageAsync(
            new JsonRpcNotification { Method = method, Params = parameters },
            cancellationToken);
    }

    /// <summary>
    /// Sends a ping request to verify server connectivity.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the ping is successful.</returns>
    public static Task PingAsync(this IMcpClient client, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<dynamic>(
            CreateRequest("ping", null),
            cancellationToken);
    }

    /// <summary>
    /// Retrieves a sequence of available tools from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous sequence of tool information.</returns>
    public static async IAsyncEnumerable<Tool> ListToolsAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        do
        {
            var tools = await ListToolsAsync(client, cursor, cancellationToken).ConfigureAwait(false);
            foreach (var tool in tools.Tools)
            {
                yield return tool;
            }

            cursor = tools.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a sequence of available tools from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cursor">A cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the server's response with tool information.</returns>
    public static Task<ListToolsResult> ListToolsAsync(this IMcpClient client, string? cursor, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<ListToolsResult>(
            CreateRequest("tools/list", CreateCursorDictionary(cursor)),
            cancellationToken);
    }

    /// <summary>
    /// Retrieves a list of available prompts from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous sequence of prompt information.</returns>
    public static async IAsyncEnumerable<Prompt> ListPromptsAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        do
        {
            var prompts = await ListPromptsAsync(client, cursor, cancellationToken).ConfigureAwait(false);
            foreach (var prompt in prompts.Prompts)
            {
                yield return prompt;
            }

            cursor = prompts.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a list of available prompts from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cursor">A  cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the server's response with prompt information.</returns>
    public static Task<ListPromptsResult> ListPromptsAsync(this IMcpClient client, string? cursor, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<ListPromptsResult>(
            CreateRequest("prompts/list", CreateCursorDictionary(cursor)),
            cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific prompt with optional arguments.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="name">The name of the prompt to retrieve</param>
    /// <param name="arguments">Optional arguments for the prompt</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the prompt's content and messages.</returns>
    public static Task<GetPromptResult> GetPromptAsync(this IMcpClient client, string name, Dictionary<string, object>? arguments = null, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<GetPromptResult>(
            CreateRequest("prompts/get", CreateParametersDictionary(name, arguments)),
            cancellationToken);
    }

    /// <summary>
    /// Retrieves a sequence of available resources from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An asynchronous sequence of resource information.</returns>
    public static async IAsyncEnumerable<Resource> ListResourcesAsync(
        this IMcpClient client, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        do
        {
            var resources = await ListResourcesAsync(client, cursor, cancellationToken).ConfigureAwait(false);
            foreach (var resource in resources.Resources)
            {
                yield return resource;
            }

            cursor = resources.NextCursor;
        }
        while (cursor is not null);
    }

    /// <summary>
    /// Retrieves a list of available resources from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="cursor">A  cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task<ListResourcesResult> ListResourcesAsync(this IMcpClient client, string? cursor, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<ListResourcesResult>(
            CreateRequest("resources/list", CreateCursorDictionary(cursor)),
            cancellationToken);
    }

    /// <summary>
    /// Reads a resource from the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task<ReadResourceResult> ReadResourceAsync(this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<ReadResourceResult>(
            CreateRequest("resources/read", new() { ["uri"] = uri }),
            cancellationToken);
    }

    /// <summary>
    /// Gets the completion options for a resource or prompt reference and (named) argument.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="reference">A resource (uri) or prompt (name) reference</param>
    /// <param name="argumentName">Name of argument. Must be non-null and non-empty.</param>
    /// <param name="argumentValue">Value of argument. Must be non-null.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task<CompleteResult> GetCompletionAsync(this IMcpClient client, Reference reference, string argumentName, string argumentValue, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);
        Throw.IfNull(reference);
        Throw.IfNullOrWhiteSpace(argumentName);

        if (!reference.Validate(out string? validationMessage))
        {
            throw new ArgumentException($"Invalid reference: {validationMessage}", nameof(reference));
        }

        return client.SendRequestAsync<CompleteResult>(
            CreateRequest("completion/complete", new()
            {
                ["ref"] = reference,
                ["argument"] = new Argument { Name = argumentName, Value = argumentValue }
            }),
            cancellationToken);
    }

    /// <summary>
    /// Subscribes to a resource on the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task SubscribeToResourceAsync(this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<dynamic>(
            CreateRequest("resources/subscribe", new() { ["uri"] = uri }),
            cancellationToken);
    }

    /// <summary>
    /// Unsubscribes from a resource on the server.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static Task UnsubscribeFromResourceAsync(this IMcpClient client, string uri, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<dynamic>(
            CreateRequest("resources/unsubscribe", new() { ["uri"] = uri }),
            cancellationToken);
    }

    /// <summary>
    /// Invokes a tool on the server with optional arguments.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">Optional arguments for the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the tool's response.</returns>
    public static Task<CallToolResponse> CallToolAsync(this IMcpClient client, string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken = default)
    {
        Throw.IfNull(client);

        return client.SendRequestAsync<CallToolResponse>(
            CreateRequest("tools/call", CreateParametersDictionary(toolName, arguments)),
            cancellationToken);
    }

    private static JsonRpcRequest CreateRequest(string method, Dictionary<string, object?>? parameters) =>
        new JsonRpcRequest
        {
            Method = method,
            Params = parameters
        };

    private static Dictionary<string, object?>? CreateCursorDictionary(string? cursor) =>
        cursor != null ? new() { ["cursor"] = cursor } : null;

    private static Dictionary<string, object?> CreateParametersDictionary(string nameParameter, Dictionary<string, object>? arguments)
    {
        Dictionary<string, object?> parameters = new()
        {
            ["name"] = nameParameter
        };

        if (arguments != null)
        {
            parameters["arguments"] = arguments;
        }

        return parameters;
    }
}