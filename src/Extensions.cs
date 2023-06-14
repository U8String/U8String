using System.Runtime.CompilerServices;
using System.Text;

using U8Primitives;

namespace System;

#pragma warning disable CA1305 // Specify IFormatProvider
public static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsWhitespace(this byte value)
    {
        // TODO: Implement this
        return value is (byte)' ';
    }

    public static U8String ToU8String(this string value)
    {
        return U8String.Parse(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this Span<char> value)
    {
        return ToU8String((ReadOnlySpan<char>)value);
    }

    public static U8String ToU8String(this ReadOnlySpan<char> value)
    {
        var bytes = new byte[Encoding.UTF8.GetMaxByteCount(value.Length)];
        var length = Encoding.UTF8.GetBytes(value, bytes);

        return new U8String(bytes, 0, (uint)length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this Span<byte> value)
    {
        return new U8String(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this ReadOnlySpan<byte> value)
    {
        return new U8String(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String<T>(this T value, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        return ToU8String(value, default, provider);
    }

    public static U8String ToU8String<T>(
        this T value,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        var buffer = new byte[32];

    Retry:
        // TODO: Decide whether to validate against potentially
        // incompliant IUtf8SpanFormattable implementations
        if (value.TryFormat(buffer, out var length, format, provider))
        {
            return new U8String(buffer, 0, (uint)length);
        }

        // Limits???? Check what CoreLib does
        buffer = new byte[buffer.Length * 2];
        goto Retry;
    }
}
