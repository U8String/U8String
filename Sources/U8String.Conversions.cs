using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

using U8.Shared;

namespace U8;

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

    /// <summary>
    /// Decodes this instance of <see cref="U8String"/> to specified destination buffer
    /// of UTF-16 <see cref="char"/>s.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination"/> is too small to fit the decoded string.
    /// </exception>
    public void CopyTo(Span<char> destination, out int charsWritten)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var result = Utf8.ToUtf16(
                source.UnsafeSpan,
                destination,
                out _,
                out charsWritten,
                replaceInvalidSequences: false);

            if (result is OperationStatus.DestinationTooSmall)
            {
                ThrowHelpers.ArgumentException("Destination buffer is too small.");
            }
            else
            {
                // This is intentional - we want to punish the callers which break the contract
                // and corrupt U8String instances by having this condition be undefined behavior.
                // Maybe tomorrow it will throw, maybe it will not, maybe it will partially write?
                Debug.Assert(
                    result is OperationStatus.Done,
                    "Found invalid U8String while converting to UTF-16. This should never happen.");
            }
        }
        else charsWritten = 0;
    }

    /// <summary>
    /// Decodes this instance of <see cref="U8String"/> to specified destination buffer
    /// of UTF-16 <see cref="char"/>s.
    /// </summary>
    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        var source = this;
        var success = true;
        if (!source.IsEmpty)
        {
            var result = Utf8.ToUtf16(
                source.UnsafeSpan,
                destination,
                out _,
                out charsWritten,
                replaceInvalidSequences: false);

            if (result is OperationStatus.DestinationTooSmall)
            {
                success = false;
            }
            else
            {
                Debug.Assert(
                    result is OperationStatus.Done,
                    "Found invalid U8String while converting to UTF-16. This should never happen.");
            }
        }
        else charsWritten = 0;

        return success;
    }

    /// <inheritdoc />
    static U8String IParsable<U8String>.Parse(string s, IFormatProvider? _)
    {
        return new(s);
    }

    /// <inheritdoc />
    static U8String ISpanParsable<U8String>.Parse(ReadOnlySpan<char> s, IFormatProvider? _)
    {
        // TODO: Decide when/how TryParse could fail, factor in the required codegen shape
        // to skip CNS string decoding after https://github.com/dotnet/runtime/pull/85328 lands.
        return new(s);
    }

    /// <inheritdoc />
    static U8String IUtf8SpanParsable<U8String>.Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? _)
    {
        return new(utf8Text);
    }

    /// <inheritdoc />
    static bool IParsable<U8String>.TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? _,
        [MaybeNullWhen(false)] out U8String result) => TryCreate(s, out result);

    /// <inheritdoc />
    static bool ISpanParsable<U8String>.TryParse(
        ReadOnlySpan<char> s,
        IFormatProvider? _,
        out U8String result) => TryCreate(s, out result);

    /// <inheritdoc />
    static bool IUtf8SpanParsable<U8String>.TryParse(
        ReadOnlySpan<byte> utf8Text,
        IFormatProvider? _,
        out U8String result) => TryCreate(utf8Text, out result);

    /// <inheritdoc />
    bool ISpanFormattable.TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) => TryCopyTo(destination, out charsWritten);

    /// <inheritdoc />
    bool IUtf8SpanFormattable.TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        var source = this;
        var result = true;
        bytesWritten = 0;
        if (!source.IsEmpty)
        {
            if (utf8Destination.Length >= source.Length)
            {
                source.UnsafeSpan.CopyToUnsafe(ref utf8Destination.AsRef());
                bytesWritten = source.Length;
                result = true;
            }
            else result = false;
        }

        return result;
    }

    /// <summary>
    /// Creates a new <see cref="byte"/> array from this <see cref="U8String"/>'s bytes.
    /// </summary>
    public byte[] ToArray()
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            return deref.UnsafeSpan.ToArray();
        }

        return [];
    }

    /// <summary>
    /// Encodes this instance of <see cref="U8String"/> into its UTF-16 <see cref="string"/> representation.
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

    /// <summary>
    /// Encodes this instance of <see cref="U8String"/> into its UTF-16 <see cref="string"/> representation and
    /// stores it in the decoded pool.
    /// <para>
    /// If the <see cref="U8String"/> slice is already in the decoded pool, the existing <see cref="string"/>
    /// is returned instead.
    /// </para>
    /// <remarks>
    /// Pooled <see cref="string"/>s are linked to the lifetime of <see cref="Source"/>s they were created from.
    /// Multiple <see cref="string"/>s can be linked to the same <see cref="Source"/> to represent the slices
    /// of the portions of the original data.
    /// </remarks>
    /// </summary>
    public string ToStringInterned()
    {
        if (!IsEmpty)
        {
            return U8Interning.GetDecoded(this);
        }

        return string.Empty;
    }

    /// <inheritdoc />
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }
}
