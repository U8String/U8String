using System.Buffers;
using System.Text;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref UnsafeSpan[index];
    }

    /// <inheritdoc cref="this[int]"/>
    byte IList<byte>.this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index];
        set => throw new NotImplementedException();
    }

    // TODO: Naming? Other options are ugly or long, or even more confusing.
    public bool IsRuneBoundary(int index)
    {
        return (uint)index < (uint)Length
            && !U8Info.IsContinuationByte(UnsafeRefAdd(index));
    }

    public int NextRuneIndex(int index)
    {
        var deref = this;
        if ((uint)index >= (uint)deref.Length)
        {
            return Length;
        }

        var span = deref.UnsafeSpan;
        while (index < span.Length && U8Info.IsContinuationByte(span[index]))
        {
            index++;
        }

        return index;
    }

    public Rune GetRuneAt(int index)
    {
        var deref = this;
        if ((uint)index >= (uint)deref.Length)
        {
            // TODO: EH UX
            ThrowHelpers.IndexOutOfRange();
        }

        if (U8Info.IsContinuationByte(deref.UnsafeRefAdd(index)))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return U8Conversions.CodepointToRune(ref deref.UnsafeRefAdd(index), out _);
    }

    public bool TryGetRuneAt(int index, out Rune rune)
    {
        var deref = this;
        if ((uint)index < (uint)deref.Length)
        {
            // TODO: Better codegen shape with dedicated method? (which won't dereference first byte twice,
            // or at least will optimize with dereferincing an entire word instead?)
            // TODO: Guard against surrogate values(if applicable)/range of values not accepted by Rune?
            rune = U8Conversions.CodepointToRune(ref deref.UnsafeRefAdd(index), out _);
            return !U8Info.IsContinuationByte(deref.UnsafeRefAdd(index));
        }

        rune = default;
        return false;
    }
}
