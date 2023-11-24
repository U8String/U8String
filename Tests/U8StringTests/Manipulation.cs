using System.Collections.Immutable;
using System.Text;

namespace U8Primitives.Tests.U8StringTests;

public partial class Manipulation
{
    const byte Byte = (byte)'!';
    const char OneByteChar = 'A';
    const char TwoByteChar = 'Ї';
    const char ThreeByteChar = 'こ';
    static readonly char SurrogateChar = "😂"[0];
    static readonly Rune OneByteRune = new(OneByteChar);
    static readonly Rune TwoByteRune = new(TwoByteChar);
    static readonly Rune ThreeByteRune = new(ThreeByteChar);
    static readonly Rune FourByteRune = "😂".EnumerateRunes().First();

    static readonly byte[] Empty = [];
    static readonly byte[] Latin = "Hello, World"u8.ToArray();
    static readonly byte[] Cyrillic = "Привіт, Всесвіт"u8.ToArray();
    static readonly byte[] Japanese = "こんにちは、世界"u8.ToArray();
    static readonly byte[] Emoji = "📈📈📈📈📈"u8.ToArray();
    static readonly byte[] Mixed = "HelloПривітこんにちは📈"u8.ToArray();
    static readonly byte[] Invalid = [0x80, 0x80, 0x80, 0x80];

    public static readonly object[][] Strings = [[Empty], [Latin], [Cyrillic], [Japanese], [Emoji], [Mixed]];

    [Fact]
    public void NullTerminate_NullTerminatesEmpty()
    {
        var empty = default(U8String);

        Assert.False(empty.IsNullTerminated);

        var nullTerminated = empty.NullTerminate();

        Assert.Equal(0, nullTerminated[0]);
        Assert.Equal(1, nullTerminated.Length);
        Assert.True(nullTerminated.IsNullTerminated);
    }

    [Fact]
    public void NullTerminate_NullTerminatesNotNullTerminated()
    {
        var values = (IEnumerable<byte[]>)[Latin, Cyrillic, Japanese, Emoji, Mixed];

        foreach (var value in values.Select(v => new U8String(v, 0, v.Length)))
        {
            Assert.False(value.IsNullTerminated);

            var nullTerminated = value.NullTerminate();

            Assert.True(nullTerminated.IsNullTerminated);
            Assert.False(nullTerminated.SourceEquals(value));

            Assert.Equal(value, nullTerminated[..^1]);
            Assert.Equal(value.Length, nullTerminated.Length - 1);
        }
    }

    [Fact]
    public void NullTerminate_ReturnsSourceForNullTerminated()
    {
        var value = Mixed.Append((byte)0).ToArray();
        var nullTerminatedBefore = new U8String(value, 0, value.Length);

        Assert.True(nullTerminatedBefore.IsNullTerminated);

        var nullTerminatedAfter = nullTerminatedBefore.NullTerminate();

        Assert.True(nullTerminatedAfter.IsNullTerminated);
        Assert.True(nullTerminatedBefore.SourceEquals(nullTerminatedAfter));

        Assert.Equal(nullTerminatedBefore, nullTerminatedAfter);
        Assert.Equal(nullTerminatedBefore.Offset, nullTerminatedAfter.Offset);
        Assert.Equal(nullTerminatedBefore.Length, nullTerminatedAfter.Length);
    }

    [Fact]
    public void NullTerminate_ReturnsCorrectValueForImplicitlyNullTerminated()
    {
        var value = U8String.Create(Mixed);

        Assert.True(value.IsNullTerminated);
        Assert.NotEqual(0, value[^1]);

        var nullTerminated = value.NullTerminate();

        Assert.True(nullTerminated.IsNullTerminated);
        Assert.True(nullTerminated.SourceEquals(value));

        Assert.Equal(value, nullTerminated[..^1]);
        Assert.Equal(value.Offset, nullTerminated.Offset);
        Assert.Equal(value.Length, nullTerminated.Length - 1);
    }

#pragma warning disable IDE0057 // Use range operator. Why: to pick the right overload.
    public static readonly IEnumerable<object[]> SliceData = new[]
    {
        Constants.CyrilicBytes,
        Constants.KanaBytes,
        Constants.NonSurrogateEmojiBytes
    }.Select(v => new object[] { new U8String(v) });

    [Fact]
    public void Slice_SimpleSliceProducesCorrectResult()
    {
        var value = (U8String)"Привіт, Всесвіт!"u8;

        Assert.True(value.Slice(0, 0).Equals(""u8));
        Assert.True(value.Slice(0, 0).Equals(U8String.Empty));

        Assert.True(value.Slice(value.Length).Equals(""u8));
        Assert.True(value.Slice(value.Length).Equals(U8String.Empty));

        Assert.True(value[..2].Equals("П"u8));
        Assert.True(value.Slice(0, 2).Equals("П"u8));

        Assert.True(value[2..].Equals("ривіт, Всесвіт!"u8));
        Assert.True(value.Slice(2).Equals("ривіт, Всесвіт!"u8));

        Assert.True(value[2..^1].Equals("ривіт, Всесвіт"u8));
        Assert.True(value.Slice(2, value.Length - 3).Equals("ривіт, Всесвіт"u8));

        Assert.True(value[^1..].Equals("!"u8));
        Assert.True(value.Slice(value.Length - 1).Equals("!"u8));
    }

    [Theory, MemberData(nameof(SliceData))]
    public void Slice_SlicingAtValidOffsetProducesCorrectResult(U8String value)
    {
        var (rune, _) = value.Runes;

        var validOffsets = Enumerable
            .Range(0, value.Length)
            .Where(i => i % rune.Utf8SequenceLength is 0);

        foreach (var offset in validOffsets)
        {
            var actual = value.Slice(offset);
            var expected = value.AsSpan().Slice(offset);

            Assert.True(actual.Equals(expected));
            Assert.Equal(value.Length - offset, actual.Length);
        }
    }

    [Theory, MemberData(nameof(SliceData))]
    public void Slice_SlicingAtValidOffsetValidLengthProducesCorrectResult(U8String value)
    {
        var (rune, _) = value.Runes;

        var offsets = Enumerable
            .Range(0, value.Length)
            .Where(i => i % rune.Utf8SequenceLength is 0);

        var validArgs = offsets
            .Select(o => Enumerable
                .Range(0, value.Length - o)
                .Where(l => l % rune.Utf8SequenceLength is 0)
                .Select(l => (offset: o, length: l)))
            .SelectMany(v => v);

        foreach (var (offset, length) in validArgs)
        {
            var actual = value.Slice(offset, length);
            var expected = value.AsSpan().Slice(offset, length);

            Assert.True(actual.Equals(expected));
            Assert.Equal(length, actual.Length);
        }
    }

    [Theory, MemberData(nameof(SliceData))]
    public void Slice_SlicingAtContinuationByteOffsetThrows(U8String value)
    {
        var (rune, _) = value.Runes;

        var invalidOffsets = Enumerable
            .Range(0, value.Length)
            .Where(i => i % rune.Utf8SequenceLength != 0
                && i != value.Length);

        foreach (var index in invalidOffsets)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(index));
        }
    }

    [Theory, MemberData(nameof(SliceData))]
    public void Slice_SlicingAtContinuationByteOffsetValidLengthThrows(U8String value)
    {
        var (rune, _) = value.Runes;

        var invalidOffsets = Enumerable
            .Range(0, value.Length)
            .Where(i => i % rune.Utf8SequenceLength != 0
                && i != value.Length);

        foreach (var offset in invalidOffsets)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(offset, value.Length - offset));
        }
    }

    [Theory, MemberData(nameof(SliceData))]
    public void Slice_SlicingAtValidOffsetContinuationByteLengthThrows(U8String value)
    {
        var (rune, _) = value.Runes;

        var invalidArgs = Enumerable
            .Range(0, value.Length)
            .Where(i => i % rune.Utf8SequenceLength != 0
                && i != value.Length)
            .Select(l => Enumerable
                .Range(0, l)
                .Where(o => o % rune.Utf8SequenceLength is 0)
                .Select(o => (offset: o, length: l)))
            .SelectMany(v => v);

        foreach (var (offset, length) in invalidArgs)
        {
            _ = value.Slice(offset);
            _ = value.Slice(offset, value.Length - offset);
            Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(offset, length));
        }
    }

    [Fact]
    public void Slice_SlicingAtNegativeOffsetThrows()
    {
        var value = new U8String(Constants.CyrilicBytes);

        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(int.MinValue));
    }

    [Fact]
    public void Slice_SlicingAtNegativeOffsetValidLengthThrows()
    {
        var value = new U8String(Constants.AsciiBytes);

        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-1, value.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(-2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(int.MinValue, 1));
    }
#pragma warning restore IDE0057

    public static IEnumerable<(
        ImmutableArray<byte> expected,
        ImmutableArray<byte> actual)> TrimData()
    {
        var values = new[]
        {
            [],
            [..Constants.AsciiBytes
                .Except(Constants.AsciiWhitespaceBytes.ToArray())],
            Constants.CyrilicBytes,
            Constants.KanaBytes,
            Constants.NonSurrogateEmojiBytes,
            Constants.MixedBytes
        };

        foreach (var value in values)
        {
            yield return (value, value);

            foreach (var rune in Constants.WhitespaceRunes)
            {
                var runeBytes = rune.ToUtf8();

                yield return (value, [..runeBytes, ..value]);
                yield return (value, [..value, ..runeBytes]);
                yield return (value, [..runeBytes, ..value, ..runeBytes]);
            }

            var allRuneBytes = Constants.WhitespaceRunes
                .SelectMany(r => r.ToUtf8())
                .ToImmutableArray();

            yield return (value, [..allRuneBytes, ..value]);
            yield return (value, [..value, ..allRuneBytes]);
            yield return (value, [..allRuneBytes, ..value, ..allRuneBytes]);
        }
    }

    [Fact]
    public void Trim_ReturnsCorrectValue()
    {
        foreach (var (expected, actual) in TrimData())
        {
            var value = new U8String(actual);
            var trimmed = value.Trim();

            Assert.True(trimmed.Equals(expected));
        }
    }

    [Fact]
    public void TrimStart_ReturnsCorrectValue()
    {
        foreach (var (expected, actual) in TrimData())
        {
            var actualSpan = actual.AsSpan();
            if (expected.Length > 1)
            {
                // We don't trim end so remove that part from comparison.
                var postfixOffset = actualSpan.IndexOf(expected.AsSpan()) + expected.Length;
                actualSpan = actual.AsSpan()[..postfixOffset];
            }

            var value = new U8String(actualSpan);
            var trimmed = value.TrimStart();

            Assert.True(trimmed.Equals(expected));
        }
    }

    [Fact]
    public void TrimEnd_ReturnsCorrectValue()
    {
        foreach (var (expected, actual) in TrimData())
        {
            var actualSpan = actual.AsSpan();
            if (expected.Length > 1)
            {
                // We don't trim start so remove that part from comparison.
                var prefixOffset = actualSpan.LastIndexOf(expected.AsSpan());
                actualSpan = actualSpan[prefixOffset..];
            }

            var value = new U8String(actualSpan);
            var trimmed = value.TrimEnd();

            Assert.True(trimmed.Equals(expected));
        }
    }
}
