using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace U8.Serialization;

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
            var length = 0;
            var buffer = (byte[]?)null;

            if (!reader.HasValueSequence)
            {
                var span = reader.ValueSpan;
                if (span.Length > 0)
                {
                    var nullTerminate = span[^1] != 0;

                    length = span.Length;
                    buffer = new byte[span.Length + (nullTerminate ? 1 : 0)];
                    span.CopyToUnsafe(ref buffer.AsRef());
                }
            }
            else
            {
                var sequence = reader.ValueSequence;
                if (sequence.Length > 0)
                {
                    length = int.CreateChecked(sequence.Length);
                    buffer = new byte[length + 1];
                    sequence.CopyTo(buffer);
                }
            }

            return new(buffer, 0, length);
        }

        return JsonException(reader.TokenType);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, U8String value, JsonSerializerOptions options)
    {
        // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WriteStringIndented")]
        // static extern void WriteStringIndented(Utf8JsonWriter writer, ReadOnlySpan<byte> value);

        // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WriteStringMinimized")]
        // static extern void WriteStringMinimized(Utf8JsonWriter writer, ReadOnlySpan<byte> value);

        // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "WriteStringEscapeValue")]
        // static extern void WriteStringEscapeValue(Utf8JsonWriter writer, ReadOnlySpan<byte> value, int firstEscapeIndex);

        // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetFlagToAddListSeparatorBeforeNextItem")]
        // static extern void SetFlagToAddListSeparatorBeforeNextItem(Utf8JsonWriter writer);

        // [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_tokenType")]
        // static extern ref JsonTokenType TokenTypeRef(Utf8JsonWriter writer);

        // try
        // {
        //     var encoder = options.Encoder ?? JavaScriptEncoder.Default;
        //     var span = value.AsSpan();

        //     var escapeOffset = encoder.FindFirstCharacterToEncodeUtf8(value);
        //     if (escapeOffset < 0)
        //     {
        //         if (options.WriteIndented)
        //         {
        //             WriteStringIndented(writer, span);
        //         }
        //         else
        //         {
        //             WriteStringMinimized(writer, span);
        //         }
        //     }
        //     else
        //     {
        //         WriteStringEscapeValue(writer, span, escapeOffset);
        //     }

        //     SetFlagToAddListSeparatorBeforeNextItem(writer);
        //     TokenTypeRef(writer) = JsonTokenType.String;
        // }
        // // If you think that I care that this is a bad practice - no I don't.
        // // This will work and the blame on the way it's handled is on System.Text.Json.
        // catch
        // {
            writer.WriteStringValue(value);
        // }
    }

    [DoesNotReturn, StackTraceHidden]
    static U8String JsonException(JsonTokenType tokenType)
    {
        throw new JsonException($"Unexpected token type: {tokenType}");
    }
}
