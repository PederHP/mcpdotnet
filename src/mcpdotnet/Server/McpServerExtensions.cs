using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Server;

/// <inheritdoc />
public static class McpServerExtensions
{
    /// <summary>
    /// Requests the client to create a new message.
    /// </summary>
    public static Task<CreateMessageResult> RequestSamplingAsync(
        this IMcpServer server, CreateMessageRequestParams request, CancellationToken cancellationToken)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (server.ClientCapabilities?.Sampling is null)
        {
            throw new McpServerException("Client does not support sampling");
        }

        return server.SendRequestAsync<CreateMessageResult>(
            new JsonRpcRequest { Method = "sampling/createMessage", Params = request },
            cancellationToken);
    }

    /// <summary>
    /// Requests the client to list the roots it exposes.
    /// </summary>
    public static Task<ListRootsResult> RequestRootsAsync(
        this IMcpServer server, ListRootsRequestParams request, CancellationToken cancellationToken)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (server.ClientCapabilities?.Roots is null)
        {
            throw new McpServerException("Client does not support roots");
        }

        return server.SendRequestAsync<ListRootsResult>(
            new JsonRpcRequest { Method = "roots/list", Params = request },
            cancellationToken);
    }

    /// <summary>
    /// Sets the handler for list tools requests.
    /// </summary>
    public static void SetListToolsHandler(
        this IMcpServer server, Func<RequestContext<ListToolsRequestParams>, CancellationToken, Task<ListToolsResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.ListTools, handler);
    }

    /// <summary>
    /// Sets the handler for call tool requests.
    /// </summary>
    public static void SetCallToolHandler(
        this IMcpServer server, Func<RequestContext<CallToolRequestParams>, CancellationToken, Task<CallToolResponse>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.CallTool, handler);
    }

    /// <summary>
    /// Sets the handler for list prompts requests.
    /// </summary>
    public static void SetListPromptsHandler(
        this IMcpServer server, Func<RequestContext<ListPromptsRequestParams>, CancellationToken, Task<ListPromptsResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.ListPrompts, handler);
    }

    /// <summary>
    /// Sets the handler for get prompt requests.
    /// </summary>
    public static void SetGetPromptHandler(
        this IMcpServer server, Func<RequestContext<GetPromptRequestParams>, CancellationToken, Task<GetPromptResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.GetPrompt, handler);
    }

    /// <summary>
    /// Sets the handler for list resources requests.
    /// </summary>
    public static void SetListResourcesHandler(
        this IMcpServer server, Func<RequestContext<ListResourcesRequestParams>, CancellationToken, Task<ListResourcesResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.ListResources, handler);
    }

    /// <summary>
    /// Sets the handler for read resource requests.
    /// </summary>
    public static void SetReadResourceHandler(
        this IMcpServer server, Func<RequestContext<ReadResourceRequestParams>, CancellationToken, Task<ReadResourceResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.ReadResource, handler);
    }

    /// <summary>
    /// Sets the handler for get completion requests.
    /// </summary>
    public static void SetGetCompletionHandler(
        this IMcpServer server, Func<RequestContext<CompleteRequestParams>, CancellationToken, Task<CompleteResult>> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.GetCompletion, handler);
    }

    /// <summary>
    /// Sets the handler for subscribe to resources requests.
    /// </summary>
    public static void SetSubscribeToResourcesHandler(
        this IMcpServer server, Func<RequestContext<string>, CancellationToken, Task> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.SubscribeToResources, handler);
    }

    /// <summary>
    /// Sets the handler for unsubscribe from resources requests.
    /// </summary>
    public static void SetUnsubscribeFromResourcesHandler(
        this IMcpServer server, Func<RequestContext<string>, CancellationToken, Task> handler)
    {
        if (server is null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        server.SetOperationHandler(OperationNames.UnsubscribeFromResources, handler);
    }
}
