using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return Value != null
            ? MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length)
            : default;
    }

    ///<summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/> starting at the specified index.
    /// </summary>
    /// <param name="start">The index to start at.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="start"/> is less than zero or greater than <see cref="Length"/>.
    /// </exception>
    // Codegen for the overloads below would probably be garbage, which is ok for now.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(int start)
    {
        return Value != null
            ? MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length)[start..]
            : default;
    }

    ///<summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/>
    /// starting at the specified index and of the specified length.
    /// </summary>
    /// <param name="start">The index to start at.</param>
    /// <param name="length">The length of the span.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when either <paramref name="start"/> or <paramref name="length"/> is out of bounds.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(int start, int length)
    {
        return Value != null
            ? MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length)[start..(start + length)]
            : default;
    }

    ///<summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/> sliced by the specified <see cref="System.Range"/>.
    /// </summary>
    /// <param name="range">The range to slice <see cref="U8String"/> by.</param>.
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="range"/> is out of bounds.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan(Range range)
    {
        return Value != null
            ? MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length)[range]
            : default;
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlyMemory{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> AsMemory()
    {
        return Value != null ? Value.AsMemory(Offset, Length) : default;
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
        // Double traversal becomes the favourable tradeoff at longer lengths,
        // and at shorter lengths the overhead is negligible.
        var length = Encoding.UTF8.GetByteCount(s);
        var value = new byte[length];

        if (Encoding.UTF8.TryGetBytes(s, value, out var _))
        {
            result = new U8String(value, 0, length);
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
        var length = Length;
        if (length <= utf8Destination.Length)
        {
            AsSpan().CopyTo(utf8Destination);
            bytesWritten = length;
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray() => AsSpan().ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public override string ToString()
    {
        return !IsEmpty ? Encoding.UTF8.GetString(this) : "";
    }
}
