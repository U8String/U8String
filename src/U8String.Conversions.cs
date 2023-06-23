using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return !IsEmpty ? MemoryMarshal.CreateReadOnlySpan(ref FirstByte, (int)_length) : default;
    }

    // Codegen for the overloads below would probably be garbage, which is ok for now.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(int start)
    {
        return !IsEmpty
            ? MemoryMarshal.CreateReadOnlySpan(ref FirstByte, (int)_length)[start..]
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(int start, int length)
    {
        return !IsEmpty
            ? MemoryMarshal.CreateReadOnlySpan(ref FirstByte, (int)_length)[start..(start + length)]
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(Range range)
    {
        return !IsEmpty
            ? MemoryMarshal.CreateReadOnlySpan(ref FirstByte, (int)_length)[range]
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> AsMemory()
    {
        return !IsEmpty ? _value.AsMemory((int)_offset, (int)_length) : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Parse(string s, IFormatProvider? provider = null)
    {
        return Parse(s.AsSpan(), provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        // TODO: Decide when/how TryParse could fail, factor in 
        _ = TryParse(s, provider, out var result);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out U8String result) => TryParse(s.AsSpan(), provider, out result);

    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out U8String result)
    {
        var maxLength = Encoding.UTF8.GetMaxByteCount(s.Length);
        var value = new byte[maxLength];

        if (Encoding.UTF8.TryGetBytes(s, value, out var length))
        {
            result = new U8String(value, 0, (uint)length);
            return true;
        }

        result = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return Encoding.UTF8.TryGetChars(AsSpan(), destination, out charsWritten);
    }

    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        var length = _length;
        if (length <= utf8Destination.Length)
        {
            AsSpan().CopyTo(utf8Destination);
            bytesWritten = (int)length;
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray() => AsSpan().ToArray();

    /// <summary>
    /// A guard against ToU8String<T> where T : IUtf8SpanFormattable overload.
    /// </summary>
    public U8String ToU8String() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public override string ToString()
    {
        return !IsEmpty ? Encoding.UTF8.GetString(this) : "";
    }
}
