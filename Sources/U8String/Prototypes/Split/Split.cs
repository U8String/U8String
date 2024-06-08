using System.Collections;

using U8.Primitives;

namespace U8.Prototypes;

readonly struct Split<T> : IEnumerable<U8String>
    where T : notnull
{
    readonly U8String _source;
    readonly T _pattern;

    internal Split(U8String source, T pattern)
    {
        _source = source;
        _pattern = pattern;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_source, _pattern);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<U8String>
    {
        readonly byte[]? _bytes;
        readonly T _pattern;

        U8Range _current;
        U8Range _remainder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(U8String source, T pattern)
        {
            _bytes = source._value;
            _pattern = pattern;
            _current = default;
            _remainder = source._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_bytes, _current);
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var source = _remainder;
            if (source.Length > 0)
            {
                var match = _pattern.FindSegment(
                    _bytes!.SliceUnsafe(source.Offset, source.Length));

                var current = new U8Range(
                    source.Offset + match.SegmentOffset,
                    match.SegmentLength);

                var remainder = new U8Range(
                    source.Offset + match.RemainderOffset,
                    source.Length - match.RemainderOffset);

                if (!match.IsFound)
                {
                    current = source;
                    remainder = default;
                }

                (_current, _remainder) = (current, remainder);

                return true;
            }

            return false;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }
}
