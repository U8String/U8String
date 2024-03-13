using System.Collections.Immutable;
using System.Runtime.InteropServices;

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

    public static readonly TheoryData<string> Utf16Strings = new(
    [
        "",
        Constants.Ascii,
        Constants.Cyrilic,
        Constants.NonSurrogateEmoji,
        Constants.Mixed
    ]);

    [Theory, MemberData(nameof(Strings))]
    public void AsSpan_ProducesCorrectValue(ImmutableArray<byte> bytes)
    {
        var expected = bytes.AsSpan();
        var actual = u8(bytes).AsSpan();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsSpan_SlicedStringProducesCorrectValue()
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
    public void AsMemory_ProducesCorrectValue(ImmutableArray<byte> bytes)
    {
        var expected = bytes.AsMemory();
        var actual = u8(bytes).AsMemory();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AsMemory_SlicedStringProducesCorrectValue()
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

    [Theory, MemberData(nameof(Utf16Strings))]
    public void CopyToUtf16_ProducesCorrectValue(string expected)
    {
        var actual = new char[expected.Length];
        u8(expected).CopyTo(actual, out var charsWritten);

        Assert.Equal(expected.AsSpan(), actual);
        Assert.Equal(expected.Length, charsWritten);
    }

    [Fact]
    public void CopyToUtf16_DoesNotWritePastOutputLength()
    {
        var expected = "Hello, World!\0\0\0\0\0\0\0";

        var input = u8("Hello, World!");
        var actual = new char[20];

        input.CopyTo(actual, out var charsWritten);

        Assert.Equal(expected.AsSpan(), actual);
        Assert.Equal(13, charsWritten);
    }

    [Fact]
    public void CopyToUtf16_ThrowsOnDestinationTooSmall()
    {
        var input = u8("Hello, World!");
        var actual = new char[5];

        Assert.Throws<ArgumentException>(() => input.CopyTo(actual, out _));
    }

    [Theory, MemberData(nameof(Utf16Strings))]
    public void TryCopyToUtf16_ProducesCorrectValue(string expected)
    {
        var overloads = new[]
        {
            bool (Span<char> destination, out int charsWritten) =>
                u8(expected).TryCopyTo(destination, out charsWritten),

            bool (Span<char> destination, out int charsWritten) =>
                ((ISpanFormattable)u8(expected)).TryFormat(destination, out charsWritten, default, null)
        };

        foreach (var overload in overloads)
        {
            var actual = new char[expected.Length];
            Assert.True(overload(actual, out var charsWritten));
            Assert.Equal(expected.AsSpan(), actual);
            Assert.Equal(expected.Length, charsWritten);
        }
    }

    [Fact]
    public void TryCopyToUtf16_DoesNotWritePastOutputLength()
    {
        var expected = "Hello, World!\0\0\0\0\0\0\0";
        var input = u8("Hello, World!");

        var overloads = new[]
        {
            bool (Span<char> destination, out int charsWritten) =>
                u8(input).TryCopyTo(destination, out charsWritten),

            bool (Span<char> destination, out int charsWritten) =>
                ((ISpanFormattable)u8(input)).TryFormat(destination, out charsWritten, default, null)
        };

        foreach (var overload in overloads)
        {
            var actual = new char[20];
            Assert.True(overload(actual, out var charsWritten));
            Assert.Equal(expected.AsSpan(), actual);
            Assert.Equal(13, charsWritten);
        }
    }

    [Fact]
    public void TryCopyToUtf16_ReturnsFalseOnDestinationTooSmall()
    {
        var input = u8("Hello, World!");
        var actual = new char[5];

        var overloads = new[]
        {
            bool (Span<char> destination, out int charsWritten) =>
                input.TryCopyTo(destination, out charsWritten),

            bool (Span<char> destination, out int charsWritten) =>
                ((ISpanFormattable)input).TryFormat(destination, out charsWritten, default, null)
        };

        Assert.False(input.TryCopyTo(actual, out var charsWritten));
        Assert.Equal(5, charsWritten);
    }

    [Theory, MemberData(nameof(Strings))]
    public void TryFormat_ProducesCorrectValue(ImmutableArray<byte> bytes)
    {
        var source = u8(bytes);
        var expected = bytes.AsSpan();
        var actual = new byte[bytes.Length];
        var result = ((IUtf8SpanFormattable)source)
            .TryFormat(actual, out var bytesWritten, default, null);

        Assert.True(result);
        Assert.Equal(expected, actual);
        Assert.Equal(expected.Length, bytesWritten);
    }

    [Theory, MemberData(nameof(Strings))]
    public void TryFormat_DoesNotWritePastOutputLength(ImmutableArray<byte> bytes)
    {
        var source = u8(bytes);
        var expected = bytes.AsSpan();
        var actual = new byte[bytes.Length + 20];
        var result = ((IUtf8SpanFormattable)source)
            .TryFormat(actual, out var bytesWritten, default, null);

        Assert.True(result);
        Assert.Equal(expected, actual.AsSpan(0, expected.Length));
        Assert.Equal(expected.Length, bytesWritten);

        var hasWrittenPastOutputLength = actual
            .AsSpan(expected.Length)
            .IndexOfAnyExcept((byte)0) >= 0;

        Assert.False(hasWrittenPastOutputLength);
    }

    [Fact]
    public void TryFormat_ReturnsFalseOnDestinationTooSmall()
    {
        var source = u8("Hello, World!");
        var actual = new byte[5];

        Assert.False(((IUtf8SpanFormattable)source)
            .TryFormat(actual, out var bytesWritten, default, null));
        Assert.Equal(0, bytesWritten);
    }

    [Theory, MemberData(nameof(Strings))]
    public void ToArray_ProducesCorrectValue(ImmutableArray<byte> bytes)
    {
        var expected = bytes.AsSpan();
        var actual = u8(bytes).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToArray_SlicedStringProducesCorrectValue()
    {
        var bytes = Constants.AsciiBytes;
        var expected = bytes.AsSpan(3..^3);
        var actual = u8(bytes)[3..^3].ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToArray_LargeStringProducesCorrectValue()
    {
        var bytes = new byte[Array.MaxLength];
        bytes.AsSpan().Fill((byte)'A');

        var expected = ImmutableCollectionsMarshal.AsImmutableArray(bytes);
        var actual = u8(expected).ToArray();

        Assert.Equal(expected.AsSpan(), actual);
    }

    [Fact]
    public void ToArray_DoesNotAllocateOnEmpty()
    {
        var first = U8String.Empty.ToArray();
        var second = default(U8String).ToArray();

        Assert.Same(first, second);
    }

    [Theory, MemberData(nameof(Utf16Strings))]
    public void ToString_ProducesCorrectValue(string expected)
    {
        var regular = u8(expected).ToString();
        var formattable = ((IFormattable)u8(expected)).ToString(null, null);

        Assert.Equal(expected, regular);
        Assert.Equal(expected, formattable);
    }
}
