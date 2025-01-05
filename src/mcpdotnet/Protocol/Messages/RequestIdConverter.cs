﻿namespace McpDotNet.Protocol.Messages;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// JSON converter for RequestId that handles both string and number values.
/// </summary>
public class RequestIdConverter : JsonConverter<RequestId>
{
    /// <inheritdoc />
    public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return RequestId.FromString(reader.GetString()!);
            case JsonTokenType.Number:
                return RequestId.FromNumber(reader.GetInt64());
            default:
                throw new JsonException("RequestId must be either a string or a number");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
    {
        if (value.IsString)
            writer.WriteStringValue(value.AsString);
        else
            writer.WriteNumberValue(value.AsNumber);
    }
}