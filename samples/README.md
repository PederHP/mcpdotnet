# Samples

This directory contains example projects demonstrating how to use mcpdotnet with different LLM SDKs and platforms.

## Environment Setup

Before running the samples, you'll need to set up the following environment variables:

- `ANTHROPIC_API_KEY` - Required for the Anthropic sample
- `OPENAI_API_KEY` - Required for the MEAI/OpenAI sample

## Sample Projects

### Anthropic Integration

Located in `samples/anthropic`, this sample demonstrates integration with Anthropic's Claude API using mcpdotnet. The example shows how to:
- Configure an MCP client for use with Anthropic
- Map between MCP protocol types and Anthropic SDK types
- Handle tool invocations and responses

### Microsoft.Extensions.AI Integration

Located in `samples/microsoft.extensions.ai`, this sample shows how to use mcpdotnet with Microsoft's AI SDK. It demonstrates:
- Setting up an MCP client with MEAI
- Mapping MCP tools to MEAI function calls
- Handling streaming responses and tool invocations

## Implementation Notes

Each sample focuses on the mapping between MCP types and SDK-specific types, which is the primary integration point when working with different LLM SDKs. The samples provide reusable mapping code that can serve as a starting point for your own implementations.

The complexity of this mapping varies depending on how closely the SDK's design aligns with typical LLM APIs. The provided samples demonstrate best practices for both straightforward and more complex mapping scenarios.

## Running the Samples

1. Clone the repository
2. Set up the required environment variables
3. Navigate to the desired sample directory
4. Run `dotnet run` to execute the sample

For more detailed examples of mcpdotnet usage, you can also refer to the integration tests in the `tests/McpDotNet.IntegrationTests` project.