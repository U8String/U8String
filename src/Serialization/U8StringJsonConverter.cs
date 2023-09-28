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
        if (reader.TokenType is JsonTokenType.String)
        {
            var buffer = !reader.HasValueSequence
                ? reader.ValueSpan.ToArray()
                : reader.ValueSequence.ToArray();
            var length = buffer.Length;

            return new(buffer, 0, length);
        }

        return JsonException(reader.TokenType);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, U8String value, JsonSerializerOptions options)
    {
        // It would be really nice to be able to skip validation here
        // but that would require either modifying JsonSerializerOptions
        // which we can't do because users don't expect it to get transiently changed
        // nor can we tell the writer to skip validation either since there is no overload for that
        // which leaves us with a choice to either contributing performance fix for utf8 string validation
        // to System.Text.Json hoping it would get accepted, possibly adjusting the overloads and impl choices
        // or copying the string to an intermediate buffer, doing escaping and adding quotes manually, and only
        // then passing the final result to the writer
        // byte[]? toReturn = null;

        // TODO: Check if the value needs to be escaped and calculate the escaped length,
        // would probably then need another pass to find+replace characters as a single action,
        // // maybe optimize for the common case of no escaping needed and then grow the buffer if needed?
        // var length = value.Length + 2;
        // var buffer = length <= 256
        //     ? stackalloc byte[256]
        //     : (toReturn = ArrayPool<byte>.Shared.Rent(length));

        // buffer[0] = (byte)'"';
        // value.AsSpan().CopyTo(buffer[1..]);
        // buffer[length - 1] = (byte)'"';

        // writer.WriteRawValue(buffer[..length], skipInputValidation: true);

        // if (toReturn != null) ArrayPool<byte>.Shared.Return(toReturn);
        writer.WriteStringValue(value);
    }

    [DoesNotReturn, StackTraceHidden]
    static U8String JsonException(JsonTokenType tokenType)
    {
        throw new JsonException($"Unexpected token type: {tokenType}");
    }
}
