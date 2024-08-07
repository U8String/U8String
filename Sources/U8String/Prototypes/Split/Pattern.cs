using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using U8.Primitives;
using U8.Shared;

namespace U8.Prototypes;

// On RegexPattern: there will be a pattern iterator abstraction.
interface Pattern {
    public static ByteLookupPattern AsciiWhitespace { get; } = new("\t\n\v\f\r "u8);
    public static ByteLookupPattern AsciiUpper { get; } = new("ABCDEFGHIJKLMNOPQRSTUVWXYZ"u8);
    public static ByteLookupPattern AsciiLower { get; } = new("abcdefghijklmnopqrstuvwxyz"u8);

    Match Find(bytes source);
    Match FindLast(bytes source);
}

interface ExtendedPattern: Pattern {
    int? Length { get; }
    bool Contains(bytes source);
    int Count(bytes source);
}

[SkipLocalsInit]
static class PatternExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountSegments<T>(this T pattern, bytes source) {
        return pattern switch {
            byte b => source.Count(b) + 1,
            char c => CountCharSplit(source, c) + 1,
            Rune r => CountRuneSplit(source, r) + 1,
            ExtendedPattern => ((ExtendedPattern)pattern).Count(source) + 1,
            Pattern => CountPatternSplit(source, pattern) + 1,
            Splitter => ((Splitter)pattern).CountSegments(source),
            _ => Unsupported<T, int>(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CountCharSplit(bytes source, char c) {
        return c <= 0x7F
            ? source.Count((byte)c) + 1
            : source.Count(c <= 0x7FF ? c.AsTwoBytes() : c.AsThreeBytes()) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int CountRuneSplit(bytes source, Rune r) {
        return (uint)r.Value <= 0x7F
            ? source.Count((byte)r.Value) + 1
            : source.Count(r.Value switch {
                <= 0x7FF => r.AsTwoBytes(),
                <= 0xFFFF => r.AsThreeBytes(),
                _ => r.AsFourBytes()
            }) + 1;
    }

    static int CountPatternSplit<T>(bytes source, T pattern) {
        Debug.Assert(pattern is Pattern);
        throw new NotImplementedException();
    }

    // TODO: Split Segment-returning primitives and Pattern path
    // from the Splitter path which returns SegmentMatch that has stack spills.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SegmentMatch FindSegment<T>(this T pattern, bytes source) {
        return pattern switch {
            byte b => new Match(source.IndexOf(b), 1),
            char c => FindChar(source, c),
            Rune r => FindRune(source, r),
            Splitter => ((Splitter)pattern).FindSegment(source),
            Pattern => ((Pattern)pattern).Find(source),
            _ => Unsupported<T, SegmentMatch>(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Match FindChar(bytes source, char c) {
        if (c <= 0x7F) {
            return new(source.IndexOf((byte)c), 1);
        }

        var needle = (bytes)(c <= 0x7FF
            ? c.AsTwoBytes()
            : c.AsThreeBytes());
        return new(source.IndexOf(needle), needle.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Match FindRune(bytes source, Rune r) {
        if (r.Value <= 0x7F) {
            return new(source.IndexOf((byte)r.Value), 1);
        }

        var needle = (bytes)(r.Value switch {
            <= 0x7FF => r.AsTwoBytes(),
            <= 0xFFFF => r.AsThreeBytes(),
            _ => r.AsFourBytes()
        });
        return new(source.IndexOf(needle), needle.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static SegmentMatch NextSegmentNonAscii<T>(this T pattern, bytes source) {
        Debug.Assert(pattern is not byte);
        return pattern switch {
            char c => FindNonAsciiChar(source, c),
            Rune r => FindNonAsciiRune(source, r),
            Splitter => ((Splitter)pattern).FindSegment(source),
            Pattern => ((Pattern)pattern).Find(source),
            _ => Unsupported<T, SegmentMatch>(),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Match FindNonAsciiChar(bytes source, char c) {
        Debug.Assert(c > 0x7F);
        var needle = (bytes)(c <= 0x7FF
            ? c.AsTwoBytes()
            : c.AsThreeBytes());
        return new(source.IndexOf(needle), needle.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Match FindNonAsciiRune(bytes source, Rune r) {
        var needle = (bytes)(r.Value switch {
            <= 0x7FF => r.AsTwoBytes(),
            <= 0xFFFF => r.AsThreeBytes(),
            _ => r.AsFourBytes()
        });
        return new(source.IndexOf(needle), needle.Length);
    }

    [DoesNotReturn, StackTraceHidden]
    static U Unsupported<T, U>() {
        throw new NotSupportedException($"{typeof(T)} is not a supported pattern type.");
    }
}

interface Splitter {
    SegmentMatch FindSegment(bytes source);
    SegmentMatch FindLastSegment(bytes source);
}

interface ExtendedSplitter: Splitter {
    bool ContainsSegment(bytes source);
    int CountSegments(bytes source);
    int FillSegments(Span<U8Range> segments, bytes source);
    int FillSegments(Span<U8String> segments, bytes source);
}

readonly struct SkipEmptySplitter<T>: Splitter
where T: struct {
    readonly T _pattern;

    public SkipEmptySplitter(T pattern) {
        ThrowHelpers.CheckPattern(pattern);
        _pattern = pattern;
    }

    public SegmentMatch FindSegment(bytes source) {
        var offset = 0;
        do {
            var match = _pattern.FindSegment(source.SliceUnsafe(offset));
            if (match.SegmentLength > 0) {
                return new(
                    match.SegmentOffset + offset,
                    match.SegmentLength,
                    match.RemainderOffset + offset);
            }
            if (!match.IsFound) break;
            offset += match.RemainderOffset;
        } while (offset < source.Length);
        return SegmentMatch.NotFound;
    }

    public SegmentMatch FindLastSegment(bytes source) {
        throw new NotImplementedException();
    }
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
    public bool IsFound => SegmentLength > -1;
    public readonly int SegmentOffset = segmentOffset;
    public readonly int SegmentLength = segmentLength;
    public readonly int RemainderOffset = remainderOffset;

    public static SegmentMatch NotFound => new(0, -1, 0);

    public void Deconstruct(out int segmentOffset, out int segmentLength, out int remainderOffset) {
        segmentOffset = SegmentOffset;
        segmentLength = SegmentLength;
        remainderOffset = RemainderOffset;
    }
}

readonly struct EitherBytePattern: ExtendedPattern {
    readonly byte _a, _b;

    public EitherBytePattern(byte a, byte b) {
        ThrowHelpers.CheckAscii((byte)(a | b));
        (_a, _b) = (a, b);
    }

    public int? Length => 1;

    public bool Contains(bytes source) {
        throw new NotImplementedException();
    }

    public int Count(bytes source) {
        return (int)(uint)U8Searching.CountEitherByte(
            _a, _b, ref source.AsRef(), (uint)source.Length);
    }

    public Match Find(bytes source) => new(source.IndexOfAny(_a, _b), 1);
    public Match FindLast(bytes source) => new(source.LastIndexOfAny(_a, _b), 1);
}

readonly struct ByteLookupPattern: Pattern {
    readonly SearchValues<byte> _values;

    public ByteLookupPattern(bytes values) {
        if (!Ascii.IsValid(values)) {
            ThrowHelpers.InvalidAscii();
        }
        _values = SearchValues.Create(values);
    }

    public Match Find(bytes source) => new(source.IndexOfAny(_values), 1);
    public Match FindLast(bytes source) => new(source.LastIndexOfAny(_values), 1);
}

// readonly struct PatternSplitter<T> where T : Pattern...
// readonly struct SplitEnumerator<TSplitter> where TSplitter : Splitter...
// Split<TPattern> -> SplitEnumerator<PatternSplitter<TPattern>>
