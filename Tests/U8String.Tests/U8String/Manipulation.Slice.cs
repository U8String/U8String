namespace U8.Tests.U8StringTests;

public partial class Manipulation
{
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
            .Flatten();

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
            .Flatten();

        foreach (var (offset, length) in invalidArgs)
        {
            _ = value.Slice(offset);
            _ = value.Slice(offset, value.Length - offset);
            Assert.Throws<ArgumentOutOfRangeException>(() => value.Slice(offset, length));
        }
    }

    [Fact]
    public void Slice_SlicingEmptyStringWithZeroOffsetSucceeds()
    {
        var value = default(U8String);
        var actual = value.Slice(0);

        Assert.Equal(0, actual.Length);
    }

    [Fact]
    public void Slice_SlicingEmptyStringWithZeroOffsetZeroLengthSucceeds()
    {
        var value = default(U8String);
        var actual = value.Slice(0, 0);

        Assert.Equal(0, actual.Offset);
        Assert.Equal(0, actual.Length);
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

    [Fact]
    public void SliceRounding_TwoByteCharactersSliceAtOffsetRoundsCorrectly()
    {
        var values = new[]
        {
            u8(Constants.CyrilicBytes),
            u8(Constants.CyrilicBytes)[2..^2]
        };

        foreach (var value in values)
        {
            for (var i = 1; i < value.Length - 1; i += 2)
            {
                var expected = value.Slice(i + 1);
                var actual = value.SliceRounding(i);

                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void SliceRounding_TwoByteCharactersSliceLengthRoundsCorrectly()
    {
        var values = new[]
        {
            u8(Constants.CyrilicBytes),
            u8(Constants.CyrilicBytes)[2..^2]
        };

        foreach (var value in values)
        {
            for (var l = value.Length - 1; l > 0; l -= 2)
            {
                var expected = value.Slice(0, l - 1);
                var actual = value.SliceRounding(0, l);

                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void SliceRounding_TwoBytesCharactersSliceAtOffsetAndLengthRoundsCorrectly()
    {
        var values = new[]
        {
            u8(Constants.CyrilicBytes),
            u8(Constants.CyrilicBytes)[2..^2]
        };

        foreach (var value in values)
        {
            for (var i = 1; i < value.Length - 1; i += 2)
            {
                for (var l = value.Length - i; l > 0; l -= 2)
                {
                    var expected = value.Slice(i + 1, l - 1);
                    var actual = value.SliceRounding(i, l);

                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    [Theory, MemberData(nameof(Strings))]
    public void SliceRounding_SliceAtInvalidOffsetRoundsCorrectly(byte[] bytes)
    {
        var value = u8(bytes);

        Assert.Equal(value, value.SliceRounding(-1));
        Assert.Equal(value, value.SliceRounding(-2));
        Assert.Equal(value, value.SliceRounding(int.MinValue / 2));
        Assert.Equal(value, value.SliceRounding(int.MinValue));

        Assert.Equal(U8String.Empty, value.SliceRounding(value.Length));
        Assert.Equal(U8String.Empty, value.SliceRounding(value.Length + 1));
        Assert.Equal(U8String.Empty, value.SliceRounding(int.MaxValue / 2));
        Assert.Equal(U8String.Empty, value.SliceRounding(int.MaxValue));
    }

    [Theory, MemberData(nameof(Strings))]
    public void SliceRounding_SliceAtOffsetLengthInvalidRoundsCorrectly(byte[] bytes)
    {
        var value = u8(bytes);

        Assert.Equal(value, value.SliceRounding(-1, value.Length));
        Assert.Equal(value, value.SliceRounding(-2, value.Length));
        Assert.Equal(value, value.SliceRounding(int.MinValue / 2, value.Length));
        Assert.Equal(value, value.SliceRounding(int.MinValue, value.Length));

        Assert.Equal(U8String.Empty, value.SliceRounding(-1, -1));
        Assert.Equal(U8String.Empty, value.SliceRounding(0, -1));
        Assert.Equal(U8String.Empty, value.SliceRounding(1, -1));
        Assert.Equal(U8String.Empty, value.SliceRounding(int.MaxValue / 2, -1));
        Assert.Equal(U8String.Empty, value.SliceRounding(int.MaxValue, -1));
        Assert.Equal(U8String.Empty, value.SliceRounding(int.MaxValue, int.MinValue));
    }
#pragma warning restore IDE0057
}