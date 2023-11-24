using System.Text;

using U8Primitives.InteropServices;

namespace U8Primitives.Tests.U8StringTests;

public partial class Manipulation
{
    [Theory, MemberData(nameof(Strings))]
    public void ConcatByte_ProducesCorrectValue(byte[] source)
    {
        var u8str = U8Marshal.Create(source);
        var actualRight = u8str + Byte;
        var expectedRight = source.Append(Byte).ToArray();

        Assert.True(actualRight.Equals(expectedRight));
        Assert.True(actualRight.IsNullTerminated);

        var actualLeft = Byte + u8str;
        var expectedLeft = source.Prepend(Byte).ToArray();

        Assert.True(actualLeft.Equals(expectedLeft));
        Assert.True(actualLeft.IsNullTerminated);
    }

    [Fact]
    public void ConcatByte_ThrowsOnInvalidByte()
    {
        var u8str = (U8String)"Hello, World!"u8;

        Assert.Throws<ArgumentOutOfRangeException>(() => u8str + 0x80);
        Assert.Throws<ArgumentOutOfRangeException>(() => 0x80 + u8str);
    }

    public static IEnumerable<object[]> CharConcats()
    {
        foreach (var value in Strings
            .Select(s => (byte[])s[0])
            .Select(Encoding.UTF8.GetChars))
        {
            yield return [value, OneByteChar];
            yield return [value, TwoByteChar];
            yield return [value, ThreeByteChar];
        }
    }

    [Theory, MemberData(nameof(CharConcats))]
    public void ConcatChar_ProducesCorrectValue(char[] source, char c)
    {
        var u8str = new U8String(source);
        var actualRight = u8str + c;
        var expectedRight = Encoding.UTF8.GetBytes(
            source.Append(c).ToArray());

        Assert.True(actualRight.Equals(expectedRight));
        Assert.True(actualRight.IsNullTerminated);

        var actualLeft = c + u8str;
        var expectedLeft = Encoding.UTF8.GetBytes(
            source.Prepend(c).ToArray());

        Assert.True(actualLeft.Equals(expectedLeft));
        Assert.True(actualLeft.IsNullTerminated);
    }

    [Fact]
    public void ConcatChar_ThrowsOnSurrogate()
    {
        var u8str = (U8String)"Hello, World!"u8;

        Assert.Throws<ArgumentException>(() => u8str + SurrogateChar);
        Assert.Throws<ArgumentException>(() => SurrogateChar + u8str);
    }

    public static IEnumerable<object[]> RuneConcats()
    {
        foreach (var value in Strings
            .Select(s => (byte[])s[0])
            .Select(Encoding.UTF8.GetString))
        {
            yield return [value, OneByteRune];
            yield return [value, TwoByteRune];
            yield return [value, ThreeByteRune];
            yield return [value, FourByteRune];
        }
    }

    [Theory, MemberData(nameof(RuneConcats))]
    public void ConcatRune_ProducesCorrectValue(string source, Rune r)
    {
        static byte[] ToBytes(IEnumerable<Rune> runes) =>
            runes.SelectMany(Extensions.ToUtf8).ToArray();

        var u8str = new U8String(source);
        var actualRight = u8str + r;
        var expectedRight = ToBytes(source.EnumerateRunes().Append(r));

        Assert.True(actualRight.Equals(expectedRight));
        Assert.True(actualRight.IsNullTerminated);

        var actualLeft = r + u8str;
        var expectedLeft = ToBytes(source.EnumerateRunes().Prepend(r));

        Assert.True(actualLeft.Equals(expectedLeft));
        Assert.True(actualLeft.IsNullTerminated);
    }

    [Fact]
    public void ConcatString_ProducesCorrectValue()
    {
        var u8str = U8Marshal.Create(Mixed);
        var actual = u8str + u8str;
        var expected = Mixed.Concat(Mixed).ToArray();

        Assert.True(actual.Equals(expected));
        Assert.True(actual.IsNullTerminated);
    }

    [Fact]
    public void ConcatString_LeftEmptyReturnsRight()
    {
        var left = (U8String)""u8;
        var right = (U8String)"Hello, World!"u8;

        var result = left + right;

        Assert.True(result.Equals(right));
        Assert.True(result.SourceEquals(right));
    }

    [Fact]
    public void ConcatString_RightEmptyReturnsLeft()
    {
        var left = (U8String)"Hello, World!"u8;
        var right = (U8String)""u8;

        var result = left + right;

        Assert.True(result.Equals(left));
        Assert.True(result.SourceEquals(left));
    }

    [Theory, MemberData(nameof(Strings))]
    public void ConcatArray_ProducesCorrectValue(byte[] source)
    {
        var u8str = U8Marshal.Create(source);
        var actualRight = u8str + Mixed;
        var expectedRight = source.Concat(Mixed).ToArray();

        Assert.True(actualRight.Equals(expectedRight));
        Assert.True(actualRight.IsNullTerminated);

        var actualLeft = Mixed + u8str;
        var expectedLeft = Mixed.Concat(source).ToArray();

        Assert.True(actualLeft.Equals(expectedLeft));
        Assert.True(actualLeft.IsNullTerminated);
    }

    [Fact]
    public void ConcatArray_LeftEmptyReturnsRight()
    {
        var left = Array.Empty<byte>();
        var right = (U8String)"Hello, World!"u8;

        var result = left + right;

        Assert.True(result.Equals(right));
        Assert.True(result.SourceEquals(right));
    }

    [Fact]
    public void ConcatArray_RightEmptyReturnsLeft()
    {
        var left = (U8String)"Hello, World!"u8;
        var right = Array.Empty<byte>();

        var result = left + right;

        Assert.True(result.Equals(left));
        Assert.True(result.SourceEquals(left));
    }

    [Fact]
    public void ConcatArray_ThrowsOnInvalid()
    {
        var u8str = (U8String)"Hello, World!"u8;

        Assert.Throws<FormatException>(() => u8str + Invalid);
        Assert.Throws<FormatException>(() => Invalid + u8str);
    }

    [Theory, MemberData(nameof(Strings))]
    public void ConcatSpan_ProducesCorrectValue(byte[] source)
    {
        var u8str = U8Marshal.Create(source);
        var actualRight = u8str + Mixed.AsSpan();
        var expectedRight = source.Concat(Mixed).ToArray().AsSpan();

        Assert.True(actualRight.Equals(expectedRight));
        Assert.True(actualRight.IsNullTerminated);

        var actualLeft = Mixed.AsSpan() + u8str;
        var expectedLeft = Mixed.Concat(source).ToArray().AsSpan();

        Assert.True(actualLeft.Equals(expectedLeft));
        Assert.True(actualLeft.IsNullTerminated);
    }

    [Fact]
    public void ConcatSpan_LeftEmptyReturnsRight()
    {
        var left = Span<byte>.Empty;
        var right = (U8String)"Hello, World!"u8;

        var result = left + right;

        Assert.True(result.Equals(right));
        Assert.True(result.SourceEquals(right));
    }

    [Fact]
    public void ConcatSpan_RightEmptyReturnsLeft()
    {
        var left = (U8String)"Hello, World!"u8;
        var right = Span<byte>.Empty;

        var result = left + right;

        Assert.True(result.Equals(left));
        Assert.True(result.SourceEquals(left));
    }

    [Fact]
    public void ConcatSpan_ThrowsOnInvalid()
    {
        var u8str = (U8String)"Hello, World!"u8;

        Assert.Throws<FormatException>(() => u8str + Invalid.AsSpan());
        Assert.Throws<FormatException>(() => Invalid.AsSpan() + u8str);
    }
}
