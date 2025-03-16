using McpDotNet.Protocol.Messages;
using McpDotNet.Protocol.Types;
using McpDotNet.Utils;

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
        Throw.IfNull(server);

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
        Throw.IfNull(server);

        if (server.ClientCapabilities?.Roots is null)
        {
            throw new McpServerException("Client does not support roots");
        }

        return server.SendRequestAsync<ListRootsResult>(
            new JsonRpcRequest { Method = "roots/list", Params = request },
            cancellationToken);
    }
}
