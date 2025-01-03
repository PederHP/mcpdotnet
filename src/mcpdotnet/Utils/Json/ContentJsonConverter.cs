using System.Text.Json;
using System.Text.Json.Serialization;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Utils.Json;

/// <summary>
/// JSON converter for IContent that handles polymorphic deserialization based on the "type" property.
/// </summary>
public class ContentJsonConverter : JsonConverter<IContent>
{
    /// <inheritdoc/>
    public override IContent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Missing 'type' property");
        }

        var type = typeProperty.GetString();
        return type switch
        {
            "text" => JsonSerializer.Deserialize<TextContent>(root.GetRawText(), options),
            "image" => JsonSerializer.Deserialize<ImageContent>(root.GetRawText(), options),
            _ => throw new JsonException($"Unknown content type: {type}")
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IContent value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case TextContent text:
                JsonSerializer.Serialize(writer, text, options);
                break;
            case ImageContent image:
                JsonSerializer.Serialize(writer, image, options);
                break;
            default:
                throw new JsonException($"Unknown content type: {value.GetType()}");
        }
    }
}