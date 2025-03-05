using McpDotNet.Server;

namespace TestServerWithHosting.Tools;

public static class EchoTool
{
    [McpTool(Description = "Echoes the input back to the client.")]
    public static string Echo([McpParameter(true)] string message)
    {
        return "hello " + message;
    }
}
