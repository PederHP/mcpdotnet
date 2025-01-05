namespace McpDotNet.Client;

using McpDotNet.Protocol.Types;
using McpDotNet.Protocol.Messages;

/// <summary>
/// Represents an instance of the MCP client connecting to a specific server.
/// Use PingAsync, ListToolsAsync and CallToolAsync instead of SendRequestAsync for common operations.
/// SendRequestAsync is provided for custom request and those not yet covered explicitly by the client.
/// </summary>
public interface IMcpClient : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether the client has been initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the capabilities supported by the server.
    /// </summary>
    ServerCapabilities? ServerCapabilities { get; }

    /// <summary>
    /// Gets the version and implementation information of the server.
    /// </summary>
    Implementation? ServerInfo { get; }

    /// <summary>
    /// Gets or sets the handler for the server's sampling requests.
    /// </summary>
    Func<CreateMessageRequestParams, Task<CreateMessageResult>>? SamplingHandler { get; set; }

    /// <summary>
    /// Establishes a connection to the server.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a ping request to verify server connectivity.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the ping is successful.</returns>
    Task PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of available tools from the server.
    /// </summary>
    /// <param name="cursor">An optional cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the server's response with tool information.</returns>
    Task<ListToolsResult> ListToolsAsync(string? cursor = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a tool on the server with optional arguments.
    /// </summary>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">Optional arguments for the tool.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the tool's response.</returns>
    Task<CallToolResponse> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of available prompts from the server.
    /// </summary>
    /// <param name="cursor">An optional cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the server's response with prompt information.</returns>
    Task<ListPromptsResult> ListPromptsAsync(string? cursor = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific prompt with optional arguments.
    /// </summary>
    /// <param name="name">The name of the prompt to retrieve</param>
    /// <param name="arguments">Optional arguments for the prompt</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the prompt's content and messages.</returns>
    Task<GetPromptResult> GetPromptAsync(string name, Dictionary<string, object>? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of available resources from the server.
    /// </summary>
    /// <param name="cursor">An optional cursor to paginate the results.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns></returns>
    Task<ListResourcesResult> ListResourcesAsync(string? cursor = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a resource from the server.
    /// </summary>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns></returns>
    Task<ReadResourceResult> ReadResourceAsync(string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a resource on the server.
    /// </summary>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns></returns>
    Task SubscribeToResourceAsync(string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from a resource on the server.
    /// </summary>
    /// <param name="uri">The uri of the resource.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns></returns>
    Task UnsubscribeFromResourceAsync(string uri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic JSON-RPC request to the server.
    /// NB! This is a temporary method that is available to send not yet implemented feature messages. 
    /// Once all MCP features are implemented this will be made private, as it is purely a convenience for those who wish to implement features ahead of the library.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="request">The JSON-RPC request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the server's response.</returns>
    Task<T> SendRequestAsync<T>(JsonRpcRequest request, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Registers a handler for server notifications of a specific method.
    /// 
    /// <see cref="NotificationMethods">Constants for common notification methods</see>
    /// </summary>
    /// <param name="method">The notification method to handle.</param>
    /// <param name="handler">The async handler function to process notifications.</param>
    void OnNotification(string method, Func<JsonRpcNotification, Task> handler);
}