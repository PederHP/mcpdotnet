using McpDotNet.Protocol.Types;
using McpDotNet.Server;

namespace TestServerWithHosting.Tools;

/// <summary>
/// This tool uses depenency injection and async method
/// </summary>
[McpToolType]
public class SampleLlmTool
{
    private readonly IMcpServer _server;

    public SampleLlmTool(IMcpServer server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    [McpTool(Name = "sampleLLM", Description = "Samples from an LLM using MCP's sampling feature")]
    public async Task<string> SampleLLM([McpParameter(true, Description = "The prompt to send to the LLM")] string prompt, [McpParameter(true, Description = "Maximum number of tokens to generate")] int maxTokens, CancellationToken cancellationToken)
    {
        var samplingParams = CreateRequestSamplingParams(prompt ?? string.Empty, "sampleLLM", maxTokens);
        var sampleResult = await _server.RequestSamplingAsync(samplingParams, cancellationToken);

        return $"LLM sampling result: {sampleResult.Content.Text}";
    }

    private static CreateMessageRequestParams CreateRequestSamplingParams(string context, string uri, int maxTokens = 100)
    {
        return new CreateMessageRequestParams()
        {
            Messages = [new SamplingMessage()
                {
                    Role = Role.User,
                    Content = new Content()
                    {
                        Type = "text",
                        Text = $"Resource {uri} context: {context}"
                    }
                }],
            SystemPrompt = "You are a helpful test server.",
            MaxTokens = maxTokens,
            Temperature = 0.7f,
            IncludeContext = ContextInclusion.ThisServer
        };
    }
}
