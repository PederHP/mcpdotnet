using McpDotNet.Server;

namespace mcpdotnet.TestServer;

public static class EchoTool
{
    [McpTool(Description = "Echoes the input back to the client.")]
    public static string Echo([McpParameter(true)] string message)
    {
        return "hello " + message;
    }
}
