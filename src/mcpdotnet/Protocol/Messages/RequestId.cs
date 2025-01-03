namespace McpDotNet.Protocol.Messages;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a JSON-RPC request identifier which can be either a string or a number.
/// </summary>
[JsonConverter(typeof(RequestIdConverter))]
public readonly struct RequestId : IEquatable<RequestId>
{
    private readonly object _value;

    private RequestId(object value)
    {
        _value = value;
    }

    public static RequestId FromString(string value) => new(value);

    public static RequestId FromNumber(long value) => new(value);

    public bool IsString => _value is string;
    public bool IsNumber => _value is long;

    public string AsString => _value as string ?? throw new InvalidOperationException("RequestId is not a string");
    public long AsNumber => _value is long number ? number : throw new InvalidOperationException("RequestId is not a number");

    public override string ToString() => _value.ToString() ?? "";

    public bool Equals(RequestId other) => _value.Equals(other._value);
    public override bool Equals(object? obj) => obj is RequestId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();

    public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);
    public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);
}

/// <summary>
/// JSON converter for RequestId that handles both string and number values.
/// </summary>
public class RequestIdConverter : JsonConverter<RequestId>
{
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

    public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
    {
        if (value.IsString)
            writer.WriteStringValue(value.AsString);
        else
            writer.WriteNumberValue(value.AsNumber);
    }
}