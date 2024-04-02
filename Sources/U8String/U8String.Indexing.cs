using System.Text;

using U8.Shared;

namespace U8;

#pragma warning disable RCS1003 // Add braces. Why: manual block ordering.
public readonly partial struct U8String
{
    /// <summary>
    /// Gets the UTF-8 code unit represented as <see cref="byte"/> at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.
    /// </exception>
    /// <exception cref="NullReferenceException">Thrown when <see cref="Length"/> is zero.</exception>
    /// <returns>The <see cref="byte"/> at the specified index.</returns>
    /// <remarks>
    /// When iterating over the contents of <see cref="U8String"/>, consider using <see cref="AsSpan()"/> instead
    /// for best indexing performance.
    /// </remarks>
    public ref readonly byte this[int index]
    {
        // This will throw NRE on empty, there is nothing we can do about it
        // without sacrificing codegen quality.
        // CoreLib seems to struggle with this in a similar way:
        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Collections.Immutable/src/System/Collections/Immutable/ImmutableArray_1.Minimal.cs#L131
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref UnsafeSpan[index];
    }

    /// <inheritdoc cref="this[int]"/>
    byte IList<byte>.this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index];
        set => throw new NotSupportedException();
    }

    /// <inheritdoc cref="this[int]"/>
    byte IReadOnlyList<byte>.this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index];
    }

    /// <summary>
    /// Determines whether the specified <paramref name="index"/> is at a UTF-8 code point boundary.
    /// </summary>
    /// <param name="index">The index to check.</param>
    /// <returns>
    /// <see langword="true"/> if the specified <paramref name="index"/> is at a UTF-8 code point boundary
    /// or is equal to <see cref="Length"/> or <c>0</c>; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsRuneBoundary(int index)
    {
        var deref = this;
        if (index is 0) return true;

        return (uint)index < (uint)deref.Length
            ? U8Info.IsBoundaryByte(in deref.UnsafeRefAdd(index))
            : index == deref.Length;
    }

    /// <summary>
    /// Finds the closest index where <see cref="IsRuneBoundary(int)"/> is <see langword="true"/>
    /// at or after the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index to start searching at.</param>
    /// <remarks>
    /// If <paramref name="index"/> is greater than or equal to <see cref="Length"/>,
    /// <see cref="Length"/> is returned instead.
    /// </remarks>
    public int CeilRuneIndex(int index)
    {
        var deref = this;
        if ((uint)index < (uint)deref.Length)
        {
            ref var ptr = ref deref.UnsafeRef;
            while (index < deref.Length
                && U8Info.IsContinuationByte(ptr.Add(index)))
            {
                index++;
            }

            return index;
        }

        return deref.Length;
    }

    /// <summary>
    /// Finds the closest index where <see cref="IsRuneBoundary(int)"/> is <see langword="true"/>
    /// at or before the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index to start searching at.</param>
    /// <remarks>
    /// If <paramref name="index"/> is greater than or equal to <see cref="Length"/>,
    /// <see cref="Length"/> is returned instead.
    /// </remarks>
    public int FloorRuneIndex(int index)
    {
        var deref = this;
        if ((uint)index < (uint)deref.Length)
        {
            ref var ptr = ref deref.UnsafeRef;
            while (index > 0
                && U8Info.IsContinuationByte(ptr.Add(index)))
            {
                index--;
            }

            return index;
        }

        return deref.Length;
    }

    /// <summary>
    /// Finds the next index where <see cref="IsRuneBoundary(int)"/> is <see langword="true"/>
    /// after the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index preceding the rune to find.</param>
    /// <remarks>
    /// If <paramref name="index"/> is greater than or equal to <see cref="Length"/>,
    /// <see cref="Length"/> is returned instead.
    /// </remarks>
    public int NextRuneIndex(int index)
    {
        var deref = this;
        if ((uint)index < (uint)deref.Length)
        {
            ref var ptr = ref deref.UnsafeRef;
            while (++index < deref.Length
                && U8Info.IsContinuationByte(ptr.Add(index))) ;

            return index;
        }

        return deref.Length;
    }

    /// <summary>
    /// Retrieves the UTF-8 code point at the specified index.
    /// </summary>
    /// <param name="index">The index of the code point to retrieve.</param>
    /// <param name="runeLength">The length of the code point in UTF-8 code units.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="index"/> does not point to a valid UTF-8 code point boundary.
    /// Also thrown when <paramref name="index"/> is less than zero or greater than or equal to <see cref="Length"/>.
    /// </exception>
    public Rune GetRuneAt(int index, out int runeLength)
    {
        var deref = this;
        if ((uint)index >= (uint)deref.Length)
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentException();
        }

        ref var ptr = ref deref.UnsafeRefAdd(index);
        var b0 = ptr;

        if (U8Info.IsContinuationByte(b0))
        {
            ThrowHelpers.ArgumentException();
        }

        return U8Conversions.CodepointToRune(ref ptr, out runeLength);
    }

    /// <summary>
    /// Attempts to retrieve the UTF-8 code point at the specified index.
    /// </summary>
    /// <param name="index">The index of the code point to retrieve.</param>
    /// <param name="rune">The code point at the specified index.</param>
    /// <param name="runeLength">The length of the code point in UTF-8 code units (bytes).</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="index"/> points to a valid UTF-8 code point boundary;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGetRuneAt(int index, out Rune rune, out int runeLength)
    {
        var deref = this;
        if ((uint)index < (uint)deref.Length)
        {
            ref var ptr = ref deref.UnsafeRefAdd(index);
            if (U8Info.IsBoundaryByte(in ptr))
            {
                rune = U8Conversions.CodepointToRune(ref ptr, out runeLength);
                return true;
            }
        }

        rune = default;
        runeLength = 0;
        return false;
    }
}
