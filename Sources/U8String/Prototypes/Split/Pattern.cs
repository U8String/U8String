using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using U8.Primitives;
using U8.Shared;

namespace U8.Prototypes;

// On RegexPattern: there will be a pattern iterator abstraction.
interface Pattern {
    public Match Find(ReadOnlySpan<byte> source);
    public Match FindLast(ReadOnlySpan<byte> source);
}

interface Splitter {
    public SegmentMatch FindSegment(ReadOnlySpan<byte> source);
    public SegmentMatch FindSegmentLast(ReadOnlySpan<byte> source);
}

readonly struct Match(int offset, int length) {
    public bool IsFound => Offset >= 0;
    public readonly int Offset = offset;
    public readonly int Length = length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SegmentMatch(Match match) =>
        new(0, match.Offset, match.Offset + match.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Range(Match match) =>
        match.IsFound ? new(match.Offset, match.Length) : default;
}

// SplitMatch?
readonly struct SegmentMatch(
    int segmentOffset,
    int segmentLength,
    int remainderOffset) {
    public bool IsFound => SegmentLength >= 0;
    public readonly int SegmentOffset = segmentOffset;
    public readonly int SegmentLength = segmentLength;
    public readonly int RemainderOffset = remainderOffset;
}

readonly struct BytePattern: Pattern {
    readonly byte _value;

    internal BytePattern(byte value) => _value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match Find(ReadOnlySpan<byte> source) => new(source.IndexOf(_value), 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match FindLast(ReadOnlySpan<byte> source) => new(source.LastIndexOf(_value), 1);
}

readonly struct CharPattern: Pattern {
    readonly byte _b0, _b1, _b2;
    readonly byte _length;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CharPattern(char value) {
        Unsafe.SkipInit(out this);
        Debug.Assert(!char.IsAscii(value));
        Debug.Assert(!char.IsSurrogate(value));

        if (value <= 0x7F) {
            _b0 = (byte)value;
            _length = 1;
        }
        else if (value <= 0xFF) {
            value.AsTwoBytes().Store(ref _b0);
            _length = 2;
        } else {
            value.AsThreeBytes().Store(ref _b0);
            _length = 3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match Find(ReadOnlySpan<byte> source) {
        var length = _length;
        var needle = MemoryMarshal.CreateReadOnlySpan(in _b0, length);
        return new(source.IndexOf(needle), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Match FindLast(ReadOnlySpan<byte> source) {
        var length = _length;
        var needle = MemoryMarshal.CreateReadOnlySpan(in _b0, length);
        return new(source.LastIndexOf(needle), length);
    }
}

static class PatternExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SegmentMatch NextSegment<T>(this T pattern, ReadOnlySpan<byte> source) => pattern switch {
        Pattern => ((Pattern)pattern).Find(source),
        Splitter => ((Splitter)pattern).FindSegment(source),
        _ => throw new NotSupportedException(),
    };
}

// readonly struct PatternSplitter<T> where T : Pattern...
// readonly struct SplitEnumerator<TSplitter> where TSplitter : Splitter...
// Split<TPattern> -> SplitEnumerator<PatternSplitter<TPattern>>

// static class Pattern {
//     public static ByteLookupPattern AsciiWhitespace { get; } = new(SearchValues.Create("\t\n\v\f\r "u8));
//     public static ByteLookupPattern AsciiUpper { get; } = new(SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8));
// }
