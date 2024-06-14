using System.Buffers;
using System.Text;

using U8.Primitives;
using U8.Shared;

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

static class Pattern
{
    public static ByteLookupPattern AsciiWhitespace { get; } = new(SearchValues.Create("\t\n\v\f\r "u8));
}

interface IPattern
{
    // TODO
    // bool UnsafeSkipBoundsCheck { get; }

    int Count(ReadOnlySpan<byte> source);
    int CountSegments(ReadOnlySpan<byte> source);

    Match Find(ReadOnlySpan<byte> source);
    Match FindLast(ReadOnlySpan<byte> source);

    SegmentMatch FindSegment(ReadOnlySpan<byte> source);
    SegmentMatch FindLastSegment(ReadOnlySpan<byte> source);
}

interface IStatefulPattern<TSelf> : IPattern
{
    TSelf AdvanceSegment();
}

interface INestedPattern<TInner> : IPattern
{
    TInner Inner { get; set; }
}

[SkipLocalsInit]
readonly struct BytePattern(byte value) : IPattern
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count(ReadOnlySpan<byte> source)
    {
        return (int)(uint)U8Searching.CountByte(value, ref source.AsRef(), (uint)source.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountSegments(ReadOnlySpan<byte> source)
    {
        return (int)(uint)U8Searching.CountByte(value, ref source.AsRef(), (uint)source.Length) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOf(value), 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match FindLast(ReadOnlySpan<byte> source)
    {
        return new(source.LastIndexOf(value), 1);
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

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly ref struct SpanPattern(ReadOnlySpan<byte> value) // : IPattern
{
    readonly ReadOnlySpan<byte> value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count(ReadOnlySpan<byte> source)
    {
        return value.Length is 1
            ? (int)(uint)U8Searching.CountByte(value[0], ref source.AsRef(), (uint)source.Length)
            : source.Count(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CountSegments(ReadOnlySpan<byte> source)
    {
        return Count(source) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOf(value), 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match FindLast(ReadOnlySpan<byte> source)
    {
        return new(source.LastIndexOf(value), 1);
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

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly struct EitherBytePattern(byte first, byte second) : IPattern
{
    public int Count(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public int CountSegments(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOfAny(first, second), 1);
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        return new(source.LastIndexOfAny(first, second), 1);
    }

    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOfAny(first, second);
        return new(
            segmentOffset: 0,
            segmentLength: index,
            remainderOffset: index + 1);
    }

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly struct ByteLookupPattern(SearchValues<byte> values) : IPattern
{
    public int Count(ReadOnlySpan<byte> source)
    {
        int index;
        var count = 0;
        while ((index = source.IndexOfAny(values)) >= 0)
        {
            source = source[(index + 1)..];
            count++;
        }
        return count;
    }

    public int CountSegments(ReadOnlySpan<byte> source)
    {
        return Count(source) + 1;
    }

    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOfAny(values), 1);
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        return new(source.LastIndexOfAny(values), 1);
    }

    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        var index = source.IndexOfAny(values);
        return new(
            segmentOffset: 0,
            segmentLength: index,
            remainderOffset: index + 1);
    }

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly struct TrimSelector<T> : IPattern
    where T : notnull
{
    readonly T _inner;

    internal TrimSelector(T inner)
    {
        _inner = inner;
    }

    public int Count(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public int CountSegments(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public Match Find(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        var match = _inner.FindSegment(source);
        // if (match.IsFound)
        // {
        //     var segment = source.SliceUnsafe(match.SegmentOffset, match.SegmentLength);
        //     var trimmed = segment.TrimStart();
        //     var offset = match.SegmentOffset + (segment.Length - trimmed.Length);
        //     return new SegmentMatch(offset, trimmed.Length, match.RemainderOffset);
        // }

        return match;
    }

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
readonly struct FilterEmptyPattern<T> : IPattern
    where T : notnull
{
    readonly T _inner;

    internal FilterEmptyPattern(T inner)
    {
        _inner = inner;
    }

    public int Count(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public int CountSegments(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public Match Find(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    public Match FindLast(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SegmentMatch FindSegment(ReadOnlySpan<byte> source)
    {
        SegmentMatch match;
        do
        {
            match = _inner.FindSegment(source);
            source = source.SliceUnsafe(match.RemainderOffset);
        } while (match.IsFound && match.SegmentLength == 0);

        return match;
    }

    public SegmentMatch FindLastSegment(ReadOnlySpan<byte> source)
    {
        throw new NotImplementedException();
    }
}

[SkipLocalsInit]
static class PatternExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<T>(
        this T pattern, ReadOnlySpan<byte> source)
        where T : notnull
    {
        return pattern switch
        {
            byte b => CountByte(b, source),
            char c => CountChar(c, source),
            Rune r => CountRune(r, source),
            U8String s => CountString(s, source),
            IPattern p => p.Count(source),
            _ => throw new NotSupportedException(),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountByte(byte value, ReadOnlySpan<byte> source)
        {
            return new BytePattern(value).Count(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountChar(char value, ReadOnlySpan<byte> source)
        {
            return value <= 0x7F
                ? new BytePattern((byte)value).Count(source)
                : new SpanPattern(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes()).Count(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountRune(Rune value, ReadOnlySpan<byte> source)
        {
            return value.Value <= 0x7F
                ? new BytePattern((byte)value.Value).Count(source)
                : new SpanPattern(value.Value switch
                {
                    <= 0x7FF => value.AsTwoBytes(),
                    <= 0xFFFF => value.AsThreeBytes(),
                    _ => value.AsFourBytes()
                }).Count(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountString(U8String value, ReadOnlySpan<byte> source)
        {
            return new SpanPattern(value).Count(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountSegments<T>(
        this T pattern, ReadOnlySpan<byte> source)
        where T : notnull
    {
        return pattern switch
        {
            byte b => CountByte(b, source),
            char c => CountChar(c, source),
            Rune r => CountRune(r, source),
            U8String s => CountString(s, source),
            IPattern p => p.CountSegments(source),
            _ => throw new NotSupportedException(),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountByte(byte value, ReadOnlySpan<byte> source)
        {
            return new BytePattern(value).CountSegments(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountChar(char value, ReadOnlySpan<byte> source)
        {
            return value <= 0x7F
                ? new BytePattern((byte)value).CountSegments(source)
                : new SpanPattern(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes()).CountSegments(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountRune(Rune value, ReadOnlySpan<byte> source)
        {
            return value.Value <= 0x7F
                ? new BytePattern((byte)value.Value).CountSegments(source)
                : new SpanPattern(value.Value switch
                {
                    <= 0x7FF => value.AsTwoBytes(),
                    <= 0xFFFF => value.AsThreeBytes(),
                    _ => value.AsFourBytes()
                }).CountSegments(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int CountString(U8String value, ReadOnlySpan<byte> source)
        {
            return new SpanPattern(value).CountSegments(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Match Find<T>(
        this T pattern, ReadOnlySpan<byte> source)
        where T : notnull
    {
        return pattern switch
        {
            byte b => FindByte(b, source),
            char c => FindChar(c, source),
            Rune r => FindRune(r, source),
            U8String s => FindString(s, source),
            IPattern p => p.Find(source),
            _ => throw new NotSupportedException(),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Match FindByte(byte value, ReadOnlySpan<byte> source)
        {
            return new(source.IndexOf(value), 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Match FindChar(char value, ReadOnlySpan<byte> source)
        {
            return value <= 0x7F
                ? new BytePattern((byte)value).Find(source)
                : new SpanPattern(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes()).Find(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Match FindRune(Rune value, ReadOnlySpan<byte> source)
        {
            return value.Value <= 0x7F
                ? new BytePattern((byte)value.Value).Find(source)
                : new SpanPattern(value.Value switch
                {
                    <= 0x7FF => value.AsTwoBytes(),
                    <= 0xFFFF => value.AsThreeBytes(),
                    _ => value.AsFourBytes()
                }).Find(source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Match FindString(U8String value, ReadOnlySpan<byte> source)
        {
            return new SpanPattern(value).Find(source);
        }
    }

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
