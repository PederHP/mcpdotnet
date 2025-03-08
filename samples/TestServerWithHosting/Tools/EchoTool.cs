using McpDotNet.Server;
using System.ComponentModel;

namespace TestServerWithHosting.Tools;

[McpToolType]
public static class EchoTool
{
    [McpTool, Description("Echoes the input back to the client.")]
    public static string Echo(string message)
    {
        return "hello " + message;
    }
}
