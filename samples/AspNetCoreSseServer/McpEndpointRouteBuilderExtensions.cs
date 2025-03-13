using McpDotNet.Protocol.Messages;
using McpDotNet.Server;
using McpDotNet.Utils.Json;

namespace AspNetCoreSseServer;

public static class McpEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapMcpSse(this IEndpointRouteBuilder endpoints)
    {
        IMcpServer? server = null;
        AspNetCoreSseMcpTransport? transport = null;
        var mcpServerFactory = endpoints.ServiceProvider.GetRequiredService<IMcpServerFactory>();
        var sseTransportLogger = endpoints.ServiceProvider.GetRequiredService<ILogger<AspNetCoreSseMcpTransport>>();

        var routeGroup = endpoints.MapGroup("");

        routeGroup.MapGet("/sse", async (HttpResponse response, CancellationToken requestAborted) =>
        {
            await using var localTransport = transport = new AspNetCoreSseMcpTransport(response.Body, sseTransportLogger);
            await using var localServer = server = mcpServerFactory.CreateServer(transport);

            await localServer.StartAsync(requestAborted);

            response.Headers.ContentType = "text/event-stream";
            response.Headers.CacheControl = "no-cache";

            try
            {
                await transport.RunAsync(requestAborted);
            }
            catch (OperationCanceledException) when (requestAborted.IsCancellationRequested)
            {
                // RequestAborted always triggers when the client disconnects before a complete response body is written,
                // but this is how SSE connections are typically closed.
            }
        });

        routeGroup.MapPost("/message", async (HttpContext context) =>
        {
            if (transport is null)
            {
                await Results.BadRequest("Connect to the /sse endpoint before sending messages.").ExecuteAsync(context);
                return;
            }

            var message = await context.Request.ReadFromJsonAsync<IJsonRpcMessage>(JsonSerializerOptionsExtensions.DefaultOptions, context.RequestAborted);
            if (message is null)
            {
                await Results.BadRequest("No message in request body.").ExecuteAsync(context);
                return;
            }

            await transport.OnMessageReceivedAsync(message, context.RequestAborted);
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            await context.Response.WriteAsync("Accepted");
        });

        return routeGroup;
    }
}
