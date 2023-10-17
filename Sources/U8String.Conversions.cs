using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

// Simplify conditional and slice expressions; use braces. Why: style + ensuring right branch ordering.
#pragma warning disable RCS1003, RCS1206, IDE0057
public readonly partial struct U8String
{
    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        // The code below is written in a way that allows JIT/ILC to optimize
        // byte[]? reference assignment to a csel/cmov, eliding the branch conditional.
        // Worst case, if it is not elided, it will be a predicted forward branch.
        var (value, offset, length) = this;
        ref var reference = ref Unsafe.NullRef<byte>();
        if (value != null) reference = ref MemoryMarshal.GetArrayDataReference(value);
        reference = ref Unsafe.Add(ref reference, (nint)(uint)offset);
        return MemoryMarshal.CreateReadOnlySpan(ref reference, length);
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
        return AsSpan().Slice(start);
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
        return AsSpan().Slice(start, length);
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlyMemory{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> AsMemory()
    {
        return _value.AsMemory(Offset, Length);
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    public static U8String Parse(string s, IFormatProvider? provider = null)
    {
        return new(s);
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    public static U8String Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        // TODO: Decide when/how TryParse could fail, factor in the required codegen shape
        // to skip CNS string decoding after https://github.com/dotnet/runtime/pull/85328 lands.
        return new(s);
    }

    /// <summary>
    /// Creates a <see cref="U8String"/> from the specified <paramref name="utf8Text"/>.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to create a <see cref="U8String"/> from.</param>
    /// <param name="_">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <returns>A new <see cref="U8String"/> created from <paramref name="utf8Text"/>.</returns>
    /// <exception cref="ArgumentException"> Thrown when <paramref name="utf8Text"/> contains invalid UTF-8.</exception>
    public static U8String Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? _ = null)
    {
        if (!TryParse(utf8Text, _, out var result))
        {
            ThrowHelpers.InvalidUtf8();
        }

        return result;
    }

    /// <inheritdoc cref="TryParse(ReadOnlySpan{char}, IFormatProvider?, out U8String)"/>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? _,
        [MaybeNullWhen(false)] out U8String result) => TryParse(s.AsSpan(), _, out result);

    /// <summary>
    /// Decodes the specified <paramref name="s"/> into a <see cref="U8String"/>.
    /// </summary>
    /// <param name="s">The UTF-16 encoded string to decode.</param>
    /// <param name="_">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <param name="result">The decoded <see cref="U8String"/>.</param>
    /// <returns>True if the <paramref name="s"/> was successfully decoded, otherwise false.</returns>
    public static bool TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? _,
        out U8String result)
    {
        // Double traversal becomes the favourable tradeoff at longer lengths,
        // and at shorter lengths the overhead is negligible.
        if (s.Length > 0)
        {
            var nullTerminate = s[^1] != 0;
            var length = Encoding.UTF8.GetByteCount(s);
            var value = new byte[nullTerminate ? length + 1 : length];

            if (Encoding.UTF8.TryGetBytes(s, value, out var bytesWritten))
            {
                result = new U8String(value, 0, bytesWritten);
                return true;
            }

            result = default;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Creates a <see cref="U8String"/> from the specified <paramref name="utf8Text"/>.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 encoded text to create a <see cref="U8String"/> from.</param>
    /// <param name="_">Defined by <see cref="ISpanParsable{U8String}"/> but not applicable to this type.</param>
    /// <param name="result">A new <see cref="U8String"/> created from <paramref name="utf8Text"/>.</param>
    /// <returns>True if the <paramref name="utf8Text"/> contains well-formed UTF-8, otherwise false.</returns>
    public static bool TryParse(
        ReadOnlySpan<byte> utf8Text,
        IFormatProvider? _,
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
    public bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        if (!IsEmpty)
        {
            return Encoding.UTF8.TryGetChars(UnsafeSpan, destination, out charsWritten);
        }

        charsWritten = 0;
        return true;
    }

    /// <inheritdoc />
    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        var source = this;

        bool result;
        var written = 0;
        if (!source.IsEmpty)
        {
            if (utf8Destination.Length >= source.Length)
            {
                source.UnsafeSpan.CopyToUnsafe(ref utf8Destination.AsRef());

                written = source.Length;
                result = true;
            }
            else result = false;
        }
        else result = true;

        bytesWritten = written;
        return result;
    }

    /// <summary>
    /// Returns a <see cref="byte"/> array containing the current <see cref="U8String"/>'s bytes.
    /// </summary>
    /// <returns>A new <see cref="byte"/> array to which the current <see cref="U8String"/>'s bytes were copied.</returns>
    public byte[] ToArray()
    {
        if (!IsEmpty)
        {
            return UnsafeSpan.ToArray();
        }

        return [];
    }

    /// <inheritdoc cref="ToString()"/>
    public string ToString(string? format, IFormatProvider? _) => ToString();

    /// <summary>
    /// Encodes the current <see cref="U8String"/> into its UTF-16 <see cref="string"/> representation.
    /// </summary>
    public override string ToString()
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            if (U8Interning.TryGetDecoded(deref, out var decoded))
            {
                return decoded;
            }

            return Encoding.UTF8.GetString(deref.UnsafeSpan);
        }

        return string.Empty;
    }
}
