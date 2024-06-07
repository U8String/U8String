using System.Collections;
using System.Diagnostics;

using U8.Primitives;

namespace U8.Prototypes;

readonly record struct Match(int offset, int length)
{
    public bool IsFound => Offset >= 0;
    public readonly int Offset = offset;
    public readonly int Length = length;
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

        public readonly U8String Current => new(_bytes, _current);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remainder = _remainder;
            if (remainder.Length > 0)
            {
                (_current, _remainder) = _pattern
                    .FindSegment(_bytes!.SliceUnsafe(remainder.Offset, remainder.Length))
                    .AsRangePair(remainder.Offset);
                return true;
            }

            return false;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }
}

interface ISearcher
{
    Match Find(ReadOnlySpan<byte> source);
    Match FindLast(ReadOnlySpan<byte> source);

    (Match Segment, int RemainderOffset) FindSegment(ReadOnlySpan<byte> source);
    (Match Segment, int RemainderOffset) FindLastSegment(ReadOnlySpan<byte> source);

    // (U8Range Segment, U8Range Remainder) FindSegment(U8Source source, U8Range slice);
}

readonly record struct ByteSearcher(byte value) : ISearcher
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
    public (Match Segment, Match Remainder) FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOf(value);
        return index >= 0
            ? (new(0, index), new(index + 1, source.Length - index - 1))
            : (new(0, source.Length), default);
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

readonly ref struct SpanSearcher(ReadOnlySpan<byte> value) // : ISearcher
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
    public (Match Segment, int RemainderOffset) FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOf(value);
        return index >= 0
            ? (new(0, index), index + 1)
            : (new(0, source.Length), default);
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

static class SearcherExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (Match Segment, Match Remainder) FindSegment<T>(
        this T pattern, ReadOnlySpan<byte> source)
        where T : notnull
    {
        return pattern switch
        {
            byte b => FindByte(source, b),
            char c => FindChar(source, c),
            _ => ((ISearcher)pattern).FindSegment(source)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Match Segment, Match Remainder) FindByte(ReadOnlySpan<byte> source, byte b)
        {
            return new ByteSearcher(b).FindSegment(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static (Match Segment, Match Remainder) FindChar(ReadOnlySpan<byte> source, char c)
        {
            return c <= 0x7F
                ? new ByteSearcher((byte)c).FindSegment(source)
                : new SpanSearcher(c <= 0x7FF ? c.AsTwoBytes() : c.AsThreeBytes()).FindSegment(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (U8Range Segment, U8Range Remainder) AsRangePair(
        this (Match Segment, Match Remainder) pair, int offset)
    {
        return (
            new(offset, pair.Segment.Length),
            new(offset + pair.Remainder.Offset, pair.Remainder.Length));
    }
}