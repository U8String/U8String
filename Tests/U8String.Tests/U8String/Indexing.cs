using System.Collections.Immutable;

namespace U8.Tests.U8StringTests;

public class Indexing
{
    public static readonly IEnumerable<object[]> ValidStrings = Constants
        .ValidStrings
        .Select(c => new object[] { c });

    public static readonly IEnumerable<object[]> IndexedStrings =
    [
        // bytes, rune length (bytes have uniform length runes)
        [Constants.AsciiBytes, 1],
        [Constants.CyrilicBytes, 2],
        [Constants.KanaBytes, 3],
        [Constants.NonSurrogateEmojiBytes, 4]
    ];

    [Theory, MemberData(nameof(ValidStrings))]
    public void Indexer_ReturnsCorrectByte(ReferenceText testCase)
    {
        var bytes = testCase.Utf8;
        var u8str = new U8String(testCase.Utf8);

        for (var i = 0; i < bytes.Length; i++)
        {
            Assert.Equal(bytes[i], u8str[i]);
            Assert.Equal(bytes[i], ((IList<byte>)u8str)[i]);
            Assert.Equal(bytes[i], ((IReadOnlyList<byte>)u8str)[i]);
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void Indexer_ReturnsCorrectByteForSlicedString(ImmutableArray<byte> bytes, int stride)
    {
        for (var i = 0; i < bytes.Length; i += stride)
        {
            var expected = bytes.AsSpan()[i..];
            var u8str = new U8String(bytes)[i..];

            for (var j = 0; j < expected.Length; j++)
            {
                Assert.Equal(expected[j], u8str[j]);
                Assert.Equal(expected[j], ((IList<byte>)u8str)[j]);
                Assert.Equal(expected[j], ((IReadOnlyList<byte>)u8str)[j]);
            }
        }
    }

    [Fact]
    public void Indexer_ThrowsNREOnEmptyString()
    {
        var u8str = default(U8String);

        Assert.Throws<NullReferenceException>(() => u8str[0]);
        Assert.Throws<NullReferenceException>(() => u8str[1]);
        Assert.Throws<NullReferenceException>(() => u8str[-1]);
        Assert.Throws<NullReferenceException>(() => u8str[int.MinValue]);
        Assert.Throws<NullReferenceException>(() => u8str[int.MaxValue]);

        Assert.Throws<NullReferenceException>(() => ((IList<byte>)u8str)[0]);
        Assert.Throws<NullReferenceException>(() => ((IList<byte>)u8str)[1]);
        Assert.Throws<NullReferenceException>(() => ((IList<byte>)u8str)[-1]);
        Assert.Throws<NullReferenceException>(() => ((IList<byte>)u8str)[int.MinValue]);
        Assert.Throws<NullReferenceException>(() => ((IList<byte>)u8str)[int.MaxValue]);

        Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<byte>)u8str)[0]);
        Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<byte>)u8str)[1]);
        Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<byte>)u8str)[-1]);
        Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<byte>)u8str)[int.MinValue]);
        Assert.Throws<NullReferenceException>(() => ((IReadOnlyList<byte>)u8str)[int.MaxValue]);
    }

    [Fact]
    public void Indexer_ThrowsAtInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Throws<IndexOutOfRangeException>(() => u8str[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[~u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[u8str.Length + 1]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[u8str.Length * 2]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[int.MinValue]);
        Assert.Throws<IndexOutOfRangeException>(() => u8str[int.MaxValue]);

        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[~u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[u8str.Length + 1]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[u8str.Length * 2]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[int.MinValue]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IList<byte>)u8str)[int.MaxValue]);

        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[~u8str.Length]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[u8str.Length + 1]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[u8str.Length * 2]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[int.MinValue]);
        Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<byte>)u8str)[int.MaxValue]);
    }

    [Fact]
    public void IListIndexer_ThrowsOnSet()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Throws<NotSupportedException>(() => ((IList<byte>)u8str)[0] = 0);
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void IsRuneBoundary_ReturnsTrueForBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        for (var i = 0; i < bytes.Length; i += stride)
        {
            Assert.True(u8str.IsRuneBoundary(i));
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void IsRuneBoundary_ReturnsFalseForNonBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        foreach (var index in Enumerable
            .Range(0, u8str.Length)
            .Where(i => i % stride != 0))
        {
            Assert.False(u8str.IsRuneBoundary(index));
        }
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void IsRuneBoundary_ReturnsTrueForStringEnd(ReferenceText text)
    {
        var u8str = new U8String(text.Utf8);

        Assert.True(u8str.IsRuneBoundary(u8str.Length));
    }

    [Fact]
    public void IsRuneBoundary_ReturnsTrueForStartOfEmptyString()
    {
        var u8str = default(U8String);

        Assert.True(u8str.IsRuneBoundary(0));
    }

    [Fact]
    public void IsRuneBoundary_ReturnsFalseForInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.False(u8str.IsRuneBoundary(-1));
        Assert.False(u8str.IsRuneBoundary(u8str.Length + 1));
        Assert.False(u8str.IsRuneBoundary(u8str.Length * 2));
        Assert.False(u8str.IsRuneBoundary(int.MinValue));
        Assert.False(u8str.IsRuneBoundary(int.MaxValue));
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void CeilRuneIndex_ReturnsBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        for (var i = 0; i < bytes.Length; i += stride)
        {
            Assert.Equal(i, u8str.CeilRuneIndex(i));
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void CeilRuneIndex_ReturnsNextBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        foreach (var index in Enumerable
            .Range(0, u8str.Length)
            .Where(i => i % stride != 0))
        {
            var expected = index + (stride - (index % stride));
            Assert.Equal(expected, u8str.CeilRuneIndex(index));
        }
    }

    [Fact]
    public void CeilRuneIndex_ReturnsLengthForInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Equal(u8str.Length, u8str.CeilRuneIndex(-1));
        Assert.Equal(u8str.Length, u8str.CeilRuneIndex(u8str.Length + 1));
        Assert.Equal(u8str.Length, u8str.CeilRuneIndex(u8str.Length * 2));
        Assert.Equal(u8str.Length, u8str.CeilRuneIndex(int.MinValue));
        Assert.Equal(u8str.Length, u8str.CeilRuneIndex(int.MaxValue));
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void FloorRuneIndex_ReturnsBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        for (var i = 0; i < bytes.Length; i += stride)
        {
            Assert.Equal(i, u8str.FloorRuneIndex(i));
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void FloorRuneIndex_ReturnsPreviousBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        foreach (var index in Enumerable
            .Range(0, u8str.Length)
            .Where(i => i % stride != 0))
        {
            var expected = index - (index % stride);
            Assert.Equal(expected, u8str.FloorRuneIndex(index));
        }
    }

    [Fact]
    public void FloorRuneIndex_ReturnsLengthForInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Equal(u8str.Length, u8str.FloorRuneIndex(-1));
        Assert.Equal(u8str.Length, u8str.FloorRuneIndex(u8str.Length + 1));
        Assert.Equal(u8str.Length, u8str.FloorRuneIndex(u8str.Length * 2));
        Assert.Equal(u8str.Length, u8str.FloorRuneIndex(int.MinValue));
        Assert.Equal(u8str.Length, u8str.FloorRuneIndex(int.MaxValue));
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void NextRuneIndex_ReturnsNextBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        for (var i = 0; i < bytes.Length; i++)
        {
            var expected = i + (stride - (i % stride));
            Assert.Equal(expected, u8str.NextRuneIndex(i));
        }
    }

    [Fact]
    public void NextRuneIndex_ReturnsLengthForInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Equal(u8str.Length, u8str.NextRuneIndex(-1));
        Assert.Equal(u8str.Length, u8str.NextRuneIndex(u8str.Length));
        Assert.Equal(u8str.Length, u8str.NextRuneIndex(u8str.Length + 1));
        Assert.Equal(u8str.Length, u8str.NextRuneIndex(u8str.Length * 2));
        Assert.Equal(u8str.Length, u8str.NextRuneIndex(int.MinValue));
        Assert.Equal(u8str.Length, u8str.NextRuneIndex(int.MaxValue));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void GetRuneAt_ReturnsCorrectRune(ReferenceText text)
    {
        var u8str = new U8String(text.Utf8);

        foreach (var rune in text.Runes)
        {
            var runeBytes = rune.ToUtf8();
            var runeOffset = u8str.IndexOf(runeBytes);

            Assert.Equal(rune, u8str.GetRuneAt(runeOffset, out var runeLength));
            Assert.Equal(runeBytes.Length, runeLength);
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void GetRuneAt_ThrowsAtContinuationByte(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        foreach (var index in Enumerable
            .Range(0, u8str.Length)
            .Where(i => i % stride != 0))
        {
            Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(index, out _));
        }
    }

    [Fact]
    public void GetRuneAt_ThrowsAtInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(-1, out _));
        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(u8str.Length, out _));
        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(u8str.Length + 1, out _));
        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(u8str.Length * 2, out _));
        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(int.MinValue, out _));
        Assert.Throws<ArgumentException>(() => u8str.GetRuneAt(int.MaxValue, out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void TryGetRuneAt_ReturnsTrueForBoundaryIndex(ReferenceText text)
    {
        var u8str = new U8String(text.Utf8);

        foreach (var rune in text.Runes)
        {
            var runeBytes = rune.ToUtf8();
            var runeOffset = u8str.IndexOf(runeBytes);
            var result = u8str.TryGetRuneAt(runeOffset, out var actual, out var runeLength);

            Assert.True(result);
            Assert.Equal(rune, actual);
            Assert.Equal(runeBytes.Length, runeLength);
        }
    }

    [Theory, MemberData(nameof(IndexedStrings))]
    public void TryGetRuneAt_ReturnsFalseForNonBoundaryIndex(ImmutableArray<byte> bytes, int stride)
    {
        var u8str = new U8String(bytes);

        foreach (var index in Enumerable
            .Range(0, u8str.Length)
            .Where(i => i % stride != 0))
        {
            Assert.False(u8str.TryGetRuneAt(index, out _, out _));
        }
    }

    [Fact]
    public void TryGetRuneAt_ReturnsFalseForInvalidIndices()
    {
        var u8str = new U8String(Constants.AsciiBytes);

        Assert.False(u8str.TryGetRuneAt(-1, out _, out _));
        Assert.False(u8str.TryGetRuneAt(u8str.Length, out _, out _));
        Assert.False(u8str.TryGetRuneAt(u8str.Length + 1, out _, out _));
        Assert.False(u8str.TryGetRuneAt(u8str.Length * 2, out _, out _));
        Assert.False(u8str.TryGetRuneAt(int.MinValue, out _, out _));
        Assert.False(u8str.TryGetRuneAt(int.MaxValue, out _, out _));
    }
}
