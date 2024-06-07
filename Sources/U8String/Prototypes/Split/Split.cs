using System.Collections;
using System.Text;

using U8.Primitives;

namespace U8.Prototypes;

readonly struct Match(int offset, int length)
{
    public bool IsFound => Offset >= 0;
    public readonly int Offset = offset;
    public readonly int Length = length;

    public static implicit operator Range(Match match) =>
        match.IsFound ? new(match.Offset, match.Length) : default;
}

readonly struct SegmentMatch(
    int segmentOffset,
    int segmentLength,
    int remainderOffset)
{
    public bool IsFound => SegmentLength >= 0;
    public readonly int SegmentOffset = segmentOffset;
    public readonly int SegmentLength = segmentLength;
    public readonly int RemainderOffset = remainderOffset;
}

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

interface IPattern
{
    Match Find(ReadOnlySpan<byte> source);
    Match FindLast(ReadOnlySpan<byte> source);

    SegmentMatch FindSegment(ReadOnlySpan<byte> source);
    SegmentMatch FindLastSegment(ReadOnlySpan<byte> source);

    // (U8Range Segment, U8Range Remainder) FindSegment(U8Source source, U8Range slice);
}

[SkipLocalsInit]
readonly struct BytePattern(byte value) : IPattern
{
    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOf(value), 1);
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOf(value);
        return new(
            segmentOffset: 0,
            segmentLength: index,
            remainderOffset: index + 1);
    }

    // public (U8Range Segment, U8Range Remainder) FindSegment(U8Source source, U8Range slice)
    // {
    //     var index = source.Value!
    //         .SliceUnsafe(slice.Offset, slice.Length)
    //         .IndexOf(value);

    //     U8Range remainder;
    //     if (index >= 0)
    //     {
    //         remainder = new(
    //             slice.Offset + index + 1,
    //             slice.Length - index - 1);
    //         slice = new(slice.Offset, index);
    //     }
    //     else remainder = new(slice.Length, 0);

    //     return (slice, remainder);
    // }

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly ref struct SpanPattern(ReadOnlySpan<byte> value) // : IPattern
{
    readonly ReadOnlySpan<byte> value = value;

    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOf(value), 1);
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOf(value);
        return new(
            segmentOffset: 0,
            segmentLength: index,
            remainderOffset: index + value.Length);
    }

    // public (U8Range Segment, U8Range Remainder) FindSegment(U8Source source, U8Range slice)
    // {
    //     var index = source.Value!
    //         .SliceUnsafe(slice.Offset, slice.Length)
    //         .IndexOf(value);

    //     U8Range remainder;
    //     if (index >= 0)
    //     {
    //         remainder = new(
    //             slice.Offset + index + 1,
    //             slice.Length - index - 1);
    //         slice = new(slice.Offset, index);
    //     }
    //     else remainder = new(slice.Length, 0);

    //     return (slice, remainder);
    // }

    public (Match Segment, Match Remainder) FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
static class SearcherExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SegmentMatch FindSegment<T>(
        this T pattern, ReadOnlySpan<byte> source)
        where T : notnull
    {
        return pattern switch
        {
            byte b => FindByte(source, b),
            char c => FindChar(source, c),
            Rune r => FindRune(source, r),
            U8String s => FindString(source, s),
            IPattern => ((IPattern)pattern).FindSegment(source),
            _ => throw new NotSupportedException(),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SegmentMatch FindByte(ReadOnlySpan<byte> source, byte b)
        {
            return new BytePattern(b).FindSegment(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SegmentMatch FindChar(ReadOnlySpan<byte> source, char c)
        {
            return c <= 0x7F
                ? new BytePattern((byte)c).FindSegment(source)
                : new SpanPattern(c <= 0x7FF ? c.AsTwoBytes() : c.AsThreeBytes()).FindSegment(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SegmentMatch FindRune(ReadOnlySpan<byte> source, Rune r)
        {
            var value = r.Value;
            return value <= 0x7F
                ? new BytePattern((byte)value).FindSegment(source)
                : new SpanPattern(value switch
                {
                    <= 0x7FF => r.AsTwoBytes(),
                    <= 0xFFFF => r.AsThreeBytes(),
                    _ => r.AsFourBytes()
                }).FindSegment(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static SegmentMatch FindString(ReadOnlySpan<byte> source, U8String s)
        {
            return new SpanPattern(s).FindSegment(source);
        }
    }
}
