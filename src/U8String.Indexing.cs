namespace U8Primitives;

public readonly partial struct U8String
{
    /// <summary>
    /// Gets a UTF-8 code unit represented as <see cref="byte"/> at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.
    /// </exception>
    /// <returns>The <see cref="byte"/> at the specified index.</returns>
    /// <remarks>
    /// Consider using <see cref="AsSpan()"/> instead when iterating over the contents of a <see cref="U8String"/>
    /// because <see cref="ReadOnlySpan{T}"/> is a priveleged type in the runtime and has better performance.
    /// </remarks>
    public ref readonly byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nint)(uint)index >= (nint)(uint)Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return ref UnsafeRefAdd(index);
        }
    }
}
