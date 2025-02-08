using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Messages;

namespace McpDotNet.Server;

/// <summary>
/// Represents a server that can communicate with a client using the MCP protocol.
/// </summary>
public interface IMcpServer : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the server has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the capabilities supported by the client.
    /// </summary>
    ClientCapabilities? ClientCapabilities { get; }

    /// <summary>
    /// Gets the version and implementation information of the client.
    /// </summary>
    Implementation? ClientInfo { get; }

    /// <summary>
    /// Starts the server and begins listening for client requests.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or sets the handler for list tools requests.
    /// </summary>
    Func<ListToolsRequestParams, CancellationToken, Task<ListToolsResult>>? ListToolsHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for call tool requests.
    /// </summary>
    Func<CallToolRequestParams, CancellationToken, Task<CallToolResponse>>? CallToolHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for list prompts requests.
    /// </summary>
    Func<ListPromptsRequestParams, CancellationToken, Task<ListPromptsResult>>? ListPromptsHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for get prompt requests.
    /// </summary>
    Func<GetPromptRequestParams, CancellationToken, Task<GetPromptResult>>? GetPromptHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for list resources requests.
    /// </summary>
    Func<ListResourcesRequestParams, CancellationToken, Task<ListResourcesResult>>? ListResourcesHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for read resources requests.
    /// </summary>
    Func<ReadResourceRequestParams, CancellationToken, Task<ReadResourceResult>>? ReadResourceHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for get resources requests.
    /// </summary>
    Func<CompleteRequestParams, CancellationToken, Task<CompleteResult>>? GetCompletionHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for subscribe to resources messages.
    /// </summary>
    Func<string, CancellationToken, Task>? SubscribeToResourcesHandler { get; set; }

    /// <summary>
    /// Gets or sets the handler for subscribe to resources messages.
    /// </summary>
    Func<string, CancellationToken, Task>? UnsubscribeFromResourcesHandler { get; set; }


    /// <summary>
    /// Requests the client to create a new message.
    /// </summary>
    Task<CreateMessageResult> RequestSamplingAsync(CreateMessageRequestParams request, CancellationToken cancellationToken);

    /// <summary>
    /// Requests the client to list the roots it exposes.
    /// </summary>
    Task<ListRootsResult> RequestRootsAsync(ListRootsRequestParams request, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a generic JSON-RPC request to the client.
    /// NB! This is a temporary method that is available to send not yet implemented feature messages. 
    /// Once all MCP features are implemented this will be made private, as it is purely a convenience for those who wish to implement features ahead of the library.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="request">The JSON-RPC request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the client's response.</returns>
    Task<T> SendRequestAsync<T>(JsonRpcRequest request, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Registers a handler for notifications of a specific method.
    /// 
    /// <see cref="NotificationMethods">Constants for common notification methods</see>
    /// </summary>
    /// <param name="method">The notification method to handle.</param>
    /// <param name="handler">The async handler function to process notifications.</param>
    void OnNotification(string method, Func<JsonRpcNotification, Task> handler);

    /// <summary>
    /// Sends a notification to the client.
    /// </summary>
    /// <param name="method">The notification method name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SendNotificationAsync(string method, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to the client with parameters.
    /// </summary>
    /// <param name="method">The notification method name.</param>
    /// <param name="parameters">The parameters to send with the notification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task SendNotificationAsync<T>(string method, T parameters, CancellationToken cancellationToken = default);
}
