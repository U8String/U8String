using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace U8Primitives.Serialization;

public sealed class U8StringJsonConverter : JsonConverter<U8String>
{
    public override U8String Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var (tokenType, isValueSpan) = (reader.TokenType, !reader.HasValueSequence);

        if (tokenType is JsonTokenType.String)
        {
            if (isValueSpan)
                return reader.ValueSpan.ToU8String();

            var buffer = reader.ValueSequence.ToArray();
            U8String.Validate(buffer);
            return new U8String(buffer, 0, (uint)buffer.Length);
        }

        return JsonException(tokenType);
    }

    public override void Write(Utf8JsonWriter writer, U8String value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    [DoesNotReturn, StackTraceHidden]
    private static U8String JsonException(JsonTokenType tokenType)
    {
        throw new JsonException($"Unexpected token type: {tokenType}");
    }
}
