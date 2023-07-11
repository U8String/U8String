namespace U8Primitives.Unsafe;

public static class U8Marshal
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without performing UTF-8 validation or copying the data.
    /// </summary>
    /// <remarks>
    /// Mutating <paramref name="value"/> after calling this method is undefined behavior
    /// and may result in data corruption.
    /// </remarks>
    public static U8String Create(byte[] value) =>
        new(value, 0, (uint)value.Length);

    /// <summary>
    /// Creates a new <see cref="U8String"/> around the given <paramref name="value"/>
    /// without performing bounds checking, UTF-8 validation or copying the data.
    /// </summary>
    /// <remarks>
    /// Mutating <paramref name="value"/> after calling this method is undefined behavior
    /// and may result in data corruption. <paramref name="offset"/> must be less than or equal
    /// <paramref name="length"/>. <paramref name="length"/> must be less than or equal to
    /// <paramref name="value"/>.Length - <paramref name="offset"/>.
    /// </remarks>
    public static U8String Create(byte[] value, uint offset, uint length) =>
        new(value, offset, length);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Substring(int)"/> which
    /// does not perform bounds checking or UTF-8 validation.
    /// </summary>
    public static U8String Substring(U8String value, uint offset) =>
        new(value._value, value._offset + offset, value._length - offset);

    /// <summary>
    /// Unsafe variant of <see cref="U8String.Substring(int, int)"/> which
    /// does not perform bounds checking or UTF-8 validation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Substring(U8String value, uint offset, uint length) =>
        new(value._value, value._offset + offset, length);
}
