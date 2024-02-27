using System.ComponentModel;
using System.Runtime.Intrinsics;

using U8.Primitives;
using U8.Shared;

namespace U8.InteropServices;

/// <summary>
/// Provides unsafe methods for creating and manipulating <see cref="U8String"/> instances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class U8Marshal
{
    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    /// <exception cref="NullReferenceException">
    /// Thrown when <see cref="U8String._value"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// This method is a variant of <see cref="U8String.AsSpan()"/> which skips length check
    /// and uncoditionally constructs the span from the underlying buffer.
    /// </remarks>
    public static ReadOnlySpan<byte> AsSpan(U8String str) => str.UnsafeSpan;

    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without performing UTF-8 validation or copying the data.
    /// </summary>
    /// <param name="value">UTF-8 buffer to construct U8String around.</param>
    /// <exception cref="NullReferenceException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Mutating <paramref name="value"/> after calling this method may result in undefined behavior
    /// or data corruption.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String CreateUnsafe(byte[] value) => new(value, 0, value.Length);

    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without checking the bounds, UTF-8 validation or copying the data.
    /// </summary>
    /// <param name="value">The UTF-8 buffer to construct U8String around.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    /// <param name="length">The number of bytes to use from <paramref name="value"/> starting at <paramref name="offset"/>.</param>
    /// <remarks>
    /// Mutating <paramref name="value"/> after calling this method may result in undefined behavior
    /// or data corruption. <paramref name="offset"/> must be less than or equal
    /// <paramref name="length"/>. <paramref name="length"/> must be less than or equal to
    /// <paramref name="value"/>.Length - <paramref name="offset"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String CreateUnsafe(byte[]? value, int offset, int length) => new(value, offset, length);

    /// <summary>
    /// Creates a new <see cref="U8SplitPair"/> representing a split of the given <paramref name="value"/>
    /// without performing bounds check or UTF-8 validation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair CreateSplitPairUnsafe(U8String value, int offset, int separatorLength)
    {
        return new(value, offset, separatorLength);
    }

    /// <summary>
    /// Counts the number of unicode code points starting at the given <paramref name="ptr"/>
    /// for the given <paramref name="length"/> of bytes.
    /// </summary>
    /// <param name="ptr">Pointer to the start of the UTF-8 byte sequence.</param>
    /// <param name="length">The number of bytes to count code points for.</param>
    /// <exception cref="NullReferenceException">
    /// Thrown when <paramref name="ptr"/> is <see langword="null"/> when <paramref name="length"/> is greater than zero.
    /// </exception>
    /// <exception cref="AccessViolationException">
    /// Thrown when <paramref name="ptr"/> is not a valid pointer or <paramref name="length"/> is greater than the maximum
    /// range of memory that can be addressed from <paramref name="ptr"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint CountRunes(ref byte ptr, nuint length)
    {
        if (length > 0)
        {
            return U8Searching.CountRunes(ref ptr, length);
        }

        return 0;
    }

    /// <inheritdoc cref="CountRunes(ref byte, nuint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nuint CountRunes(byte* ptr, nuint length)
    {
        if (length > 0)
        {
            return U8Searching.CountRunes(ref Unsafe.AsRef<byte>(ptr), length);
        }

        return 0;
    }

    /// <summary>
    /// Searches for the first occurence of a null byte starting at the given <paramref name="ptr"/>.
    /// </summary>
    public static unsafe nuint IndexOfNullByte(byte* ptr)
    {
        var start = ptr;

        // TODO: Optimize this
        while ((nuint)ptr % 16 != 0)
        {
            if (*ptr is 0)
            {
                goto Done;
            }

            ptr++;
        }

        var zeroes = Vector128<byte>.Zero;
        while (true)
        {
            var mask = Vector128.LoadAligned(ptr).Eq(zeroes);
            if (mask == zeroes)
            {
                ptr += Vector128<byte>.Count;
                continue;
            }

            ptr += mask.IndexOfMatch();
            break;
        }

    Done:
        return (nuint)ptr - (nuint)start;
    }

    /// <summary>
    /// Returns a reference to the first byte of the given <see cref="U8String"/>.
    /// </summary>
    /// <exception cref="NullReferenceException"><paramref name="value"/> is <see cref="U8String.Empty"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly byte GetReference(U8String value) => ref value.UnsafeRef;

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int)"/> which
    /// does not perform bounds check or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String SliceUnsafe(U8String value, int offset) =>
        new(value._value, value.Offset + offset, value.Length - offset);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds check or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    /// <param name="length">The number of bytes to use from <paramref name="value"/> starting at <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String SliceUnsafe(U8String value, int offset, int length) =>
        new(value._value, value.Offset + offset, length);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds check or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="range">The range of the new substring.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String SliceUnsafe(U8String value, Range range)
    {
        var length = value.Length;
        var start = range.Start.GetOffset(length);
        var end = range.End.GetOffset(length);

        return new(value._value, value.Offset + start, end - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String SliceUnsafe(U8Source source, U8Range range) => new(source.Value, range);
}
