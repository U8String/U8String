using System.Runtime.InteropServices;

using U8.Primitives;

namespace U8.InteropServices;

/// <summary>
/// Provides unsafe/unchecked methods for creating and manipulating <see cref="U8String"/> instances.
/// </summary>
public static class U8Marshal
{
    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> view of the current <see cref="U8String"/>.
    /// </summary>
    /// <param name="str"></param>
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
    public static U8String Create(byte[] value) => new(value, 0, value.Length);

    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without performing bounds checking, UTF-8 validation or copying the data.
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
    public static U8String Create(byte[]? value, int offset, int length) => new(value, offset, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe U8String CreateFromNullTerminated(byte* ptr)
    {
        return CreateFromNullTerminated(ref Unsafe.AsRef<byte>(ptr));
    }

    public static U8String CreateFromNullTerminated(ref byte ptr)
    {
        // TODO: Move into separate helper class with logic to account for PAGESIZE
        // to prevent SEGFAULTs from occuring due to SIMD reads past the end of the buffer.
        var span = MemoryMarshal.CreateReadOnlySpan(ref ptr, int.MaxValue);
        var length = span.IndexOf((byte)'\0');
        if (length < 0)
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(span.SliceUnsafe(0, length), skipValidation: true);
    }

    /// <summary>
    /// Creates a new <see cref="U8SplitPair"/> representing a split of the given <paramref name="value"/>
    /// without performing bounds check or UTF-8 validation.
    /// </summary>
    /// [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair CreateSplitPair(U8String value, int offset, int separatorLength)
    {
        return new(value, offset, separatorLength);
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
    public static U8String Slice(U8String value, int offset) =>
        new(value._value, value.Offset + offset, value.Length - offset);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds check or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    /// <param name="length">The number of bytes to use from <paramref name="value"/> starting at <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Slice(U8String value, int offset, int length) =>
        new(value._value, value.Offset + offset, length);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds check or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="range">The range of the new substring.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Slice(U8String value, Range range)
    {
        var length = value.Length;
        var start = range.Start.GetOffset(length);
        var end = range.End.GetOffset(length);

        return new(value._value, value.Offset + start, end - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Slice(U8Source source, U8Range range) => new(source.Value, range);

    /// <summary>
    /// Unholy reverse method of <see cref="U8String.Slice(int, int)"/> which
    /// restores internal offsets of the provided <see cref="U8String"/>
    /// to the maximum possible length of its underlying buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Unslice(U8String value)
    {
        if (!value.IsEmpty)
        {
            return new(value._value, 0, value._value!.Length);
        }

        return default;
    }
}
