using McpDotNet.Protocol.Transport;
using Microsoft.Extensions.Logging;

namespace McpDotNet.Server;

public class McpServerFactory
{
    private readonly IServerTransport _serverTransport;
    private readonly McpServerOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    public McpServerFactory(IServerTransport serverTransport, McpServerOptions options, ILoggerFactory loggerFactory)
    {
        _serverTransport = serverTransport;
        _options = options;
        _loggerFactory = loggerFactory;
    }
    public IMcpServer CreateServer(string? serverInstructions)
    {
        return new McpServer(_serverTransport, _options, serverInstructions, _loggerFactory);
    }
}
