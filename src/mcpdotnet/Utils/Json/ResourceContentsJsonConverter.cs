using System.Text.Json;
using System.Text.Json.Serialization;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Utils.Json;

/// <summary>
/// JSON converter for IResourceContents that handles polymorphic deserialization based on content properties.
/// </summary>
public class ResourceContentsJsonConverter : JsonConverter<IResourceContents>
{
    /// <inheritdoc/>
    public override IResourceContents? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Determine the type based on whether it has a 'text' or 'blob' property
        if (root.TryGetProperty("text", out _))
        {
            return JsonSerializer.Deserialize<TextResourceContents>(root.GetRawText(), options);
        }
        else if (root.TryGetProperty("blob", out _))
        {
            return JsonSerializer.Deserialize<BlobResourceContents>(root.GetRawText(), options);
        }

        throw new JsonException("Resource contents must have either 'text' or 'blob' property");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IResourceContents value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case TextResourceContents text:
                JsonSerializer.Serialize(writer, text, options);
                break;
            case BlobResourceContents blob:
                JsonSerializer.Serialize(writer, blob, options);
                break;
            default:
                throw new JsonException($"Unknown resource contents type: {value.GetType()}");
        }
    }
}