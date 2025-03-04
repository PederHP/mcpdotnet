using McpDotNet.Protocol.Types;
using System.Text.Json;

namespace McpDotNet.Extensions.AI.Tests;

public class AIFunctionUtilitiesTests
{
    [Fact]
    public void MapToJsonElement_WithRequiredProperties_CreatesCorrectSchema()
    {
        // Arrange
        var tool = new Tool()
        {
            Name = "calculator",
            Description = "A simple calculator",
            InputSchema = new JsonSchema
            {
                Properties = new Dictionary<string, JsonSchemaProperty>
                {
                    ["a"] = new JsonSchemaProperty { Type = "number" },
                    ["b"] = new JsonSchemaProperty { Type = "number" }
                },
                Required = ["a", "b"]
            }
        };

        // Act
        var result = AIFunctionUtilities.MapToJsonElement(tool);

        // Assert
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result.GetRawText());
        Assert.Equal("object", resultObj.GetProperty("type").GetString());
        Assert.Equal("calculator", resultObj.GetProperty("title").GetString());
        Assert.Equal("A simple calculator", resultObj.GetProperty("description").GetString());
        // Add more detailed property assertions
    }

    [Fact]
    public void MapToJsonElement_WithOptionalProperties_CreatesCorrectSchema()
    {
        // Arrange
        var tool = new Tool()
        {
            Name = "format",
            Description = "Text formatter",
            InputSchema = new JsonSchema
            {
                Properties = new Dictionary<string, JsonSchemaProperty>
                {
                    ["text"] = new JsonSchemaProperty { Type = "string" },
                    ["style"] = new JsonSchemaProperty { Type = "string" }
                },
                Required = ["text"]  // only text is required
            }
        };

        // Act
        var result = AIFunctionUtilities.MapToJsonElement(tool);

        // Assert
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result.GetRawText());
        Assert.Equal("object", resultObj.GetProperty("type").GetString());
        Assert.Equal("format", resultObj.GetProperty("title").GetString());
        Assert.Equal("Text formatter", resultObj.GetProperty("description").GetString());
        Assert.Contains("text", resultObj.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
        Assert.DoesNotContain("style", resultObj.GetProperty("required").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void MapToJsonElement_WithNullProperties_HandlesGracefully()
    {
        // Arrange
        var tool = new Tool()
        {
            Name = "minimal",
            Description = "Minimal tool",
            InputSchema = new JsonSchema
            {
                Properties = null,
                Required = null
            }
        };

        // Act
        var result = AIFunctionUtilities.MapToJsonElement(tool);

        // Assert
        var resultObj = JsonSerializer.Deserialize<JsonElement>(result.GetRawText());
        Assert.Equal("object", resultObj.GetProperty("type").GetString());
        Assert.Equal("minimal", resultObj.GetProperty("title").GetString());
        Assert.Equal("Minimal tool", resultObj.GetProperty("description").GetString());
        Assert.True(resultObj.GetProperty("properties").GetRawText() == "{}");
        Assert.Empty(resultObj.GetProperty("required").EnumerateArray());
    }
}