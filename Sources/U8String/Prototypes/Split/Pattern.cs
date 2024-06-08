using System.Buffers;
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

static class Pattern
{
    public static ByteLookupPattern AsciiWhitespace { get; } = new(SearchValues.Create("\t\n\v\f\r "u8));
}

interface IPattern
{
    Match Find(ReadOnlySpan<byte> source);
    Match FindLast(ReadOnlySpan<byte> source);

    SegmentMatch FindSegment(ReadOnlySpan<byte> source);
    SegmentMatch FindLastSegment(ReadOnlySpan<byte> source);
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

    public Match Find(ReadOnlySpan<byte> source)
    {
        return new(source.IndexOf(value), 1);
    }

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
readonly struct ByteLookupPattern(SearchValues<byte> values) : IPattern
{
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
static class PatternExtensions
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
