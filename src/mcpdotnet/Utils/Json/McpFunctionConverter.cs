using System.Text.Json.Serialization;
using System.Text.Json;
using McpDotNet.Protocol.Types;

namespace McpDotNet.Utils.Json;

/// <summary>
/// Json converter for <see cref="McpFunction"/>.
/// </summary>
public sealed class McpFunctionConverter : JsonConverter<McpFunction>
{
    /// <inheritdoc/>
    public override McpFunction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        string? name = null;
        string? description = null;
        JsonElement? inputSchema = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new JsonException("Tool name is required");
                }

                if (inputSchema != null)
                {
                    var typeProp = inputSchema!.Value.GetProperty("type");
                    if (typeProp.GetString() != "object")
                    {
                        throw new JsonException("Input schema must be an object");
                    }
                }

                return new McpFunction(name!, description, inputSchema);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString()!;
            reader.Read();

            switch (propertyName)
            {
                case "name":
                    name = reader.GetString();
                    break;
                case "description":
                    description = reader.GetString();
                    break;
                case "inputSchema":
                    inputSchema = JsonDocument.ParseValue(ref reader).RootElement;
                    break;
                default:
                    throw new JsonException("Unknown property");
            }
        }

        throw new JsonException();
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, McpFunction value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        if (value.Description != null)
        {
            writer.WriteString("description", value.Description);
        }

        if (value.JsonSchema.GetPropertyCount() > 0)
        {
            writer.WritePropertyName("inputSchema");
            value.JsonSchema.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}
