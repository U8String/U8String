using System.Collections;
using System.Text;

using U8.Primitives;

namespace U8;

#pragma warning disable IDE0032, IDE0057 // Use auto property and index operator. Why: Perf, struct layout, accuracy and codegen.
public readonly partial struct U8String
{
    /// <summary>
    /// Returns a collection of <see cref="char"/>s over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Chars Chars
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of <see cref="Rune"/>s over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Runes Runes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of rune indices over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8RuneIndices RuneIndices
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of lines over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Lines Lines
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    // Bad codegen still :(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    public struct Enumerator(U8String value) : IEnumerator<byte>
    {
        readonly byte[]? _value = value._value;
        readonly int _offset = value.Offset;
        readonly int _length = value.Length;
        int _index = -1;

        public readonly byte Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value!.AsRef(_offset + _index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = _index + 1;
            if ((uint)index < (uint)_length)
            {
                _index = index;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        readonly object IEnumerator.Current => Current;
        readonly void IDisposable.Dispose() { }
    }
}
