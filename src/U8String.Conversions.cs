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
        return MemoryMarshal.CreateSpan(ref FirstByte, (int)_length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> AsMemory()
    {
        return _value.AsMemory((int)_offset, (int)_length);
    }

    public static U8String Parse(string s, IFormatProvider? provider) => Parse(s.AsSpan(), provider);
    public static U8String Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        _ = TryParse(s, provider, out var result);
        return result;
    }

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
        var length = (uint)Encoding.UTF8.GetBytes(s, value);

        result = new U8String(value, 0, length);
        return true;
    }

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

    public byte[] ToArray()
    {
        return _value.AsSpan((int)_offset, (int)_length).ToArray();
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public override string ToString()
    {
        return _length is 0
            ? string.Empty
            : Encoding.UTF8.GetString(_value.AsSpan((int)_offset, (int)_length));
    }
}
