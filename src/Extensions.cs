using System.Runtime.CompilerServices;
using System.Text;
using U8Primitives;

namespace System;

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
        return new U8String(Encoding.UTF8.GetBytes(value));
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
}
