namespace McpDotNet.Tests.Utils;

public class MockHttpHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, Task<HttpResponseMessage>>? RequestHandler { get; set; }

    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (RequestHandler == null)
            throw new InvalidOperationException($"No {nameof(RequestHandler)} was set! Please set handler first and make request afterwards.");

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        var result = await RequestHandler.Invoke(request);

        if (cancellationToken.IsCancellationRequested)
            throw new OperationCanceledException(cancellationToken);

        return result;
    }
}
