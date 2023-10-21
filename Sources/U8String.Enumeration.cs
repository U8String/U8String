using System.Collections;

using Rune = System.Text.Rune;

namespace U8Primitives;

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
    /// </summary> <summary>
    /// This is a lazily-evaluated allocation-free collection.
    /// </summary>
    /// <value></value>
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

    public struct Enumerator : IEnumerator<byte>
    {
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;
        int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(U8String value)
        {
            _value = value._value;
            _offset = value.Offset;
            _length = value.Length;
            _index = -1;
        }

        // Still cheaper than MemoryMarshal clever variants
        public readonly byte Current => _value![_offset + _index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => (uint)(++_index) < (uint)_length;
        // {
        //     var index = _index;
        //     if (++index < _length)
        //     {
        //         // Current = Unsafe.Add(
        //         //     ref MemoryMarshal.GetArrayDataReference(_value!),
        //         //     (nint)(uint)(_offset + index));
        //         Current = _value![_offset + index];
        //         _index = index;
        //         return true;
        //     }

        //     return false;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        readonly object IEnumerator.Current => Current;
        readonly void IDisposable.Dispose() { }
    }
}
