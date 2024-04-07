using System.Collections;

using U8.Abstractions;

namespace U8.Primitives;

public readonly struct U8Slices :
    IU8Enumerable<U8Slices.Enumerator>,
    IList<U8String>
{
    internal readonly byte[]? Source;
    internal readonly U8Range[]? Ranges;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Slices(byte[]? source, U8Range[] slices)
    {
        Source = source;
        Ranges = slices;
    }

    public U8String this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(Source, Ranges![index]);
        set => throw new NotSupportedException();
    }

    public bool IsEmpty => Count <= 0;

    public int Count => Ranges?.Length ?? 0;

    bool ICollection<U8String>.IsReadOnly => true;

    // TODO: Optimize this
    public bool Contains(U8String item)
    {
        var source = Source;
        var ranges = Ranges;
        if (ranges != null)
        {
            for (var i = 0; i < ranges.Length; i++)
            {
                if (item.Equals(new U8String(source, ranges[i])))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // TODO: Optimize this
    public void CopyTo(U8String[] array, int arrayIndex)
    {
        var span = array.AsSpan()[arrayIndex..][..Count];
        var source = Source;
        var ranges = Ranges;
        ref var dst = ref span.AsRef();

        if (ranges != null)
        {
            for (var i = 0; i < ranges.Length; i++)
            {
                dst.Add(i) = new U8String(source, ranges[i]);
            }
        }
    }

    // TODO: Optimize this
    public int IndexOf(U8String item)
    {
        var source = Source;
        var ranges = Ranges;
        if (ranges != null)
        {
            for (var i = 0; i < ranges.Length; i++)
            {
                if (item.Equals(new U8String(source, ranges[i])))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public Enumerator GetEnumerator() => new(Source, Ranges ?? U8Constants.EmptyRanges);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _source;
        readonly U8Range[] _ranges;
        U8Range _current;
        int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(byte[]? source, U8Range[] ranges)
        {
            _source = source;
            _ranges = ranges;
        }

        public readonly U8String Current => new(_source, _current);

        readonly object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = _index;
            if ((uint)index < (uint)_ranges.Length)
            {
                _current = _ranges[index];
                _index = index + 1;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _current = default;
            _index = 0;
        }

        public readonly void Dispose() { }
    }

    void IList<U8String>.Insert(int index, U8String item) => throw new NotSupportedException();
    void IList<U8String>.RemoveAt(int index) => throw new NotSupportedException();
    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}
