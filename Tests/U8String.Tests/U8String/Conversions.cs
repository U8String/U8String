using System.Collections.Immutable;

using U8;
using U8.Tests;

public class Conversions
{
    public static readonly TheoryData<ImmutableArray<byte>> Strings = new(
    [
        [],
        Constants.AsciiBytes,
        Constants.CyrilicBytes,
        Constants.NonSurrogateEmojiBytes,
        Constants.MixedBytes
    ]);

    [Theory, MemberData(nameof(Strings))]
    public void AsSpan_ProducesExpectedResult(ImmutableArray<byte> bytes)
    {
        var expected = bytes.AsSpan();
        var actual = u8(bytes).AsSpan();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsSpan_SlicedStringProducesExpectedResult()
    {
        var bytes = Constants.AsciiBytes;
        var expected = bytes.AsSpan(3..^3);
        var actual = u8(bytes)[3..^3].AsSpan();

        Assert.Equal(expected, actual);
    }

    [Theory, InlineData(int.MaxValue / 2), InlineData(int.MaxValue)]
    public void AsSpan_EmptySliceWithLargeOffsetDoesNotAVE(int offset)
    {
        var slice = new U8String(null, offset, 0);

        Assert.Equal(Span<byte>.Empty, slice);
        Assert.Throws<IndexOutOfRangeException>(() => slice.AsSpan()[0]);
        Assert.Throws<NullReferenceException>(() => _ = slice.AsSpan().GetPinnableReference());
    }

    [Theory, MemberData(nameof(Strings))]
    public void AsMemory_ProducesExpectedResult(ImmutableArray<byte> bytes)
    {
        var expected = bytes.AsMemory();
        var actual = u8(bytes).AsMemory();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsMemory_SlicedStringProducesExpectedResult()
    {
        var bytes = Constants.AsciiBytes;
        var expected = bytes.AsMemory()[3..^3];
        var actual = u8(bytes)[3..^3].AsMemory();

        Assert.Equal(expected, actual);
    }

    [Theory, InlineData(int.MaxValue / 2), InlineData(int.MaxValue)]
    public void AsMemory_EmptySliceWithLargeOffsetDoesNotAVE(int offset)
    {
        var slice = new U8String(null, offset, 0);

        Assert.Equal(Memory<byte>.Empty, slice);
        Assert.Throws<IndexOutOfRangeException>(() => slice.AsMemory().Span[0]);
        Assert.Throws<NullReferenceException>(() => _ = slice.AsMemory().Span.GetPinnableReference());
    }
}
