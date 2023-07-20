namespace U8Primitives.InteropServices;

/// <summary>
/// Provides unsafe/unchecked methods for creating and manipulating <see cref="U8String"/> instances.
/// </summary>
public static class U8Marshal
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without performing UTF-8 validation or copying the data.
    /// </summary>
    /// <param name="value">UTF-8 buffer to construct U8String around.</param>
    /// <remarks>
    /// Mutating <paramref name="value"/> after calling this method is undefined behavior
    /// and may result in data corruption.
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
    /// Mutating <paramref name="value"/> after calling this method is undefined behavior
    /// and may result in data corruption. <paramref name="offset"/> must be less than or equal
    /// <paramref name="length"/>. <paramref name="length"/> must be less than or equal to
    /// <paramref name="value"/>.Length - <paramref name="offset"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Create(byte[] value, int offset, int length) => new(value, offset, length);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int)"/> which
    /// does not perform bounds checking or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Slice(U8String value, int offset) =>
        new(value._value, value.Offset + offset, value.Length - offset);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds checking or UTF-8 validation.
    /// </summary>
    /// <param name="value">The <see cref="U8String"/> to create a substring from.</param>
    /// <param name="offset">The offset into <paramref name="value"/> to start at.</param>
    /// <param name="length">The number of bytes to use from <paramref name="value"/> starting at <paramref name="offset"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Slice(U8String value, int offset, int length) =>
        new(value._value, value.Offset + offset, length);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Slice(int, int)"/> which
    /// does not perform bounds checking or UTF-8 validation.
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
}