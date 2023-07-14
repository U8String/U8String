using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace U8Primitives.Serialization;

/// <summary>
/// A <see cref="JsonConverter{T}"/> for <see cref="U8String"/>.
/// </summary>
public sealed class U8StringJsonConverter : JsonConverter<U8String>
{
    /// <inheritdoc/>
    public override U8String Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerValue = reader; // Dereference once
        if (readerValue.TokenType is JsonTokenType.String)
        {
            var buffer = !readerValue.HasValueSequence
                ? readerValue.ValueSpan.ToArray()
                : readerValue.ValueSequence.ToArray();
            var length = buffer.Length;

            return length > 0 ? new U8String(buffer, 0, length) : default;
        }

        return JsonException(readerValue.TokenType);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
