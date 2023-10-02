using System.Runtime.InteropServices.Marshalling;
using System.Text;
using U8Primitives.InteropServices;

namespace U8Primitives.Tests;

public class Manipulation
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

    static readonly byte[] Latin = "Hello, World"u8.ToArray();
    static readonly byte[] Cyrillic = "Привіт, Всесвіт"u8.ToArray();
    static readonly byte[] Japanese = "こんにちは、世界"u8.ToArray();
    static readonly byte[] Emoji = "📈📈📈📈📈"u8.ToArray();

    public static readonly object[][] Strings = [[Latin], [Cyrillic], [Japanese], [Emoji]];

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

        Assert.Throws<ArgumentOutOfRangeException>(() => u8str + SurrogateChar);
        Assert.Throws<ArgumentOutOfRangeException>(() => SurrogateChar + u8str);
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
            runes.SelectMany(TestExtensions.ToUtf8).ToArray();

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
}
