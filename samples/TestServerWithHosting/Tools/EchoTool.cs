using McpDotNet.Server;

namespace TestServerWithHosting.Tools;

[McpToolType]
public static class EchoTool
{
    [McpTool(description: "Echoes the input back to the client.")]
    public static string Echo([McpParameter(true)] string message)
    {
        return "hello " + message;
    }
}
