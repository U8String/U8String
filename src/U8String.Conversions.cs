using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace U8Primitives;

#pragma warning disable RCS1206 // Simplify conditional expressions. Why: codegen quality.
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

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Parse(string s, IFormatProvider? provider = null)
    {
        return Parse(s.AsSpan(), provider);
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        // TODO: Decide when/how TryParse could fail, factor in the required codegen shape
        // to skip CNS string decoding after https://github.com/dotnet/runtime/pull/85328 lands.
        _ = TryParse(s, provider, out var result);
        return result;
    }

    /// <summary>
    /// Creates a <see cref="U8String"/> from the specified <paramref name="utf8Text"/>.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to create a <see cref="U8String"/> from.</param>
    /// <param name="provider">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <returns>A new <see cref="U8String"/> created from <paramref name="utf8Text"/>.</returns>
    /// <exception cref="ArgumentException"> Thrown when <paramref name="utf8Text"/> contains invalid UTF-8.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider = null)
    {
        if (!TryParse(utf8Text, provider, out var result))
        {
            ThrowHelpers.InvalidUtf8();
        }

        return result;
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out U8String result) => TryParse(s.AsSpan(), provider, out result);

    /// <summary>
    /// Decodes the specified <paramref name="s"/> into a <see cref="U8String"/>.
    /// </summary>
    /// <param name="s">The UTF-16 encoded string to decode.</param>
    /// <param name="provider">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <param name="result">The decoded <see cref="U8String"/>.</param>
    /// <returns>True if the <paramref name="s"/> was successfully decoded, otherwise false.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? provider,
        out U8String result)
    {
        // Double traversal becomes the favourable tradeoff at longer lengths,
        // and at shorter lengths the overhead is negligible.
        var length = Encoding.UTF8.GetByteCount(s);
        var value = new byte[length];

        if (Encoding.UTF8.TryGetBytes(s, value, out var bytesWritten))
        {
            result = new U8String(value, 0, bytesWritten);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="U8String"/> from the specified <paramref name="utf8Text"/>.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to create a <see cref="U8String"/> from.</param>
    /// <param name="provider">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <param name="result">A new <see cref="U8String"/> created from <paramref name="utf8Text"/>.</param>
    /// <returns>True if the <paramref name="utf8Text"/> contains well-formed UTF-8, otherwise false.</returns>
    public static bool TryParse(
        ReadOnlySpan<byte> utf8Text,
        IFormatProvider? provider,
        out U8String result)
    {
        if (IsValid(utf8Text))
        {
            result = new U8String(utf8Text, skipValidation: true);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Encodes the current <see cref="U8String"/> into its UTF-16 representation and writes it to the specified <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The destination to write the UTF-16 representation to.</param>
    /// <param name="charsWritten">The number of characters written to <paramref name="destination"/>.</param>
    /// <param name="format">Defined by <see cref="ISpanFormattable"/> but not applicable to this type.</param>
    /// <param name="provider">Defined by <see cref="ISpanFormattable"/> but not applicable to this type.</param>
    /// <returns>True if the UTF-16 representation was successfully written to <paramref name="destination"/>, otherwise false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return Encoding.UTF8.TryGetChars(this, destination, out charsWritten);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Returns a <see cref="byte"/> array containing the current <see cref="U8String"/>'s bytes.
    /// </summary>
    /// <returns>A new <see cref="byte"/> array to which the current <see cref="U8String"/>'s bytes were copied.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray() => AsSpan().ToArray();

    /// <inheritdoc cref="ToString()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>
    /// Encodes the current <see cref="U8String"/> into its UTF-16 representation and returns it as <see cref="string"/>.
    /// </summary>
    public override string ToString()
    {
        return Value != null ? Encoding.UTF8.GetString(this) : "";
    }
}
