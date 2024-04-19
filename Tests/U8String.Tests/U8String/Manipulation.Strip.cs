using System.Text;

namespace U8.Tests.U8StringTests;

public partial class Manipulation
{
    public static object[][] StripData<T>(T left, T right)
        where T : IUtf8SpanFormattable =>
    [
        [u8(""), u8(""), left, right],
        [u8($"{left}"), u8(""), left, right],
        [u8($"{right}"), u8(""), left, right],
        [u8($"{left}{right}"), u8(""), left, right],
        [u8($"{left}{left}{right}"), u8($"{left}"), left, right],
        [u8($"{left}{right}{right}"), u8($"{right}"), left, right],
        [u8($"{left}{left}{right}{right}"), u8($"{left}{right}"), left, right],
        [u8("Тест"), u8("Тест"), left, right],
        [u8($"Тест{right}"), u8("Тест"), left, right],
        [u8($"{left}Тест"), u8("Тест"), left, right],
        [u8($"{left}Тест{right}"), u8("Тест"), left, right],
        [u8($"{left}{left}Тест"), u8($"{left}Тест"), left, right],
        [u8($"Тест{right}{right}"), u8($"Тест{right}"), left, right],
        [u8($"{left}{left}Тест{right}{right}"), u8($"{left}Тест{right}"), left, right],
        [u8($" Тест{right}{right} ")[1..^1], u8($"Тест{right}"), left, right],
        [u8($" {left}{left}Тест ")[1..^1], u8($"{left}Тест"), left, right],
        [u8($" {left}{left}Тест{right}{right} ")[1..^1], u8($"{left}Тест{right}"), left, right]
    ];

    public static readonly IEnumerable<object[]> StripByteData = new[]
    {
        '{', '}', '@', '\0'
    }.SelectMany(c => StripData(c, c)).WithArgsLimit(3);

    [Theory, MemberData(nameof(StripByteData))]
    public void StripByte_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        var b = (byte)c;
        var actual = value.Strip(b);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripByteData))]
    public void StripBytePrefix_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        var b = (byte)c;
        if (value.EndsWith(b))
            value = value[..^1];

        var actual = value.StripPrefix(b);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripByteData))]
    public void StripByteSuffix_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        var b = (byte)c;
        if (value.StartsWith(b))
            value = value[1..];

        var actual = value.StripSuffix(b);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StripByte_ThrowsOnNonAscii()
    {
        Assert.Throws<ArgumentException>(() => U8String.Empty.Strip(0x80));
        Assert.Throws<ArgumentException>(() => U8String.Empty.StripPrefix(0x80));
        Assert.Throws<ArgumentException>(() => U8String.Empty.StripSuffix(0x80));
    }

    public static IEnumerable<object[]> StripCharData => new[]
    {
        OneByteChar, TwoByteChar, ThreeByteChar
    }.SelectMany(c => StripData(c, c)).WithArgsLimit(3);

    [Theory, MemberData(nameof(StripCharData))]
    public void StripChar_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        var actual = value.Strip(c);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripCharData))]
    public void StripCharPrefix_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        if (value.EndsWith(c))
            value = value[..^c.Utf8Length()];

        var actual = value.StripPrefix(c);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripCharData))]
    public void StripCharSuffix_ReturnsCorrectValue(U8String value, U8String expected, char c)
    {
        if (value.StartsWith(c))
            value = value[c.Utf8Length()..];

        var actual = value.StripSuffix(c);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StripChar_ThrowsOnSurrogate()
    {
        Assert.Throws<ArgumentException>(() => U8String.Empty.Strip(SurrogateChar));
        Assert.Throws<ArgumentException>(() => U8String.Empty.StripPrefix(SurrogateChar));
        Assert.Throws<ArgumentException>(() => U8String.Empty.StripSuffix(SurrogateChar));
    }

    public static IEnumerable<object[]> StripRuneData() => new[]
    {
        OneByteRune, TwoByteRune, ThreeByteRune, FourByteRune
    }.SelectMany(r => StripData(r, r)).WithArgsLimit(3);

    [Theory, MemberData(nameof(StripRuneData))]
    public void StripRune_ReturnsCorrectValue(U8String value, U8String expected, Rune r)
    {
        var actual = value.Strip(r);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripRuneData))]
    public void StripRunePrefix_ReturnsCorrectValue(U8String value, U8String expected, Rune r)
    {
        if (value.EndsWith(r))
            value = value[..^r.Utf8SequenceLength];

        var actual = value.StripPrefix(r);

        Assert.Equal(expected, actual);
    }

    [Theory, MemberData(nameof(StripRuneData))]
    public void StripRuneSuffix_ReturnsCorrectValue(U8String value, U8String expected, Rune r)
    {
        if (value.StartsWith(r))
            value = value[r.Utf8SequenceLength..];

        var actual = value.StripSuffix(r);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> StripStringData() => new[]
    {
        u8(Empty), u8(Latin), u8(Cyrillic), u8(Japanese), u8(Emoji), u8(Mixed)
    }.SelectMany(v => StripData(v, v)).WithArgsLimit(3);

    [Theory, MemberData(nameof(StripStringData))]
    public void StripString_ReturnsCorrectValue(U8String value, U8String expected, U8String toStrip)
    {
        var overloads = new[]
        {
            value.Strip(toStrip),
            value.Strip(toStrip.AsSpan())
        };

        foreach (var actual in overloads)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory, MemberData(nameof(StripStringData))]
    public void StripStringPrefix_ReturnsCorrectValue(U8String value, U8String expected, U8String toStrip)
    {
        if (value.EndsWith(toStrip))
            value = value[..^toStrip.Length];

        var overloads = new[]
        {
            value.StripPrefix(toStrip),
            value.StripPrefix(toStrip.AsSpan())
        };

        foreach (var actual in overloads)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory, MemberData(nameof(StripStringData))]
    public void StripStringSuffix_ReturnsCorrectValue(U8String value, U8String expected, U8String toStrip)
    {
        if (value.StartsWith(toStrip))
            value = value[toStrip.Length..];

        var overloads = new[]
        {
            value.StripSuffix(toStrip),
            value.StripSuffix(toStrip.AsSpan())
        };

        foreach (var actual in overloads)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StripString_ThrowsOnInvalidUtf8()
    {
        Assert.Throws<FormatException>(() => U8String.Empty.Strip(Invalid));
        Assert.Throws<FormatException>(() => U8String.Empty.StripPrefix(Invalid));
        Assert.Throws<FormatException>(() => U8String.Empty.StripSuffix(Invalid));
    }

    public static readonly IEnumerable<object[]> StripPrefixSuffixByteData = new[]
    {
        '{', '}', '@', '\0'
    }.Permute2().FlatMap(StripData);

    [Theory, MemberData(nameof(StripPrefixSuffixByteData))]
    public void StripPrefixSuffixByte_ReturnsCorrectValue(
        U8String value,
        U8String expected,
        char prefix,
        char suffix)
    {
        var actual = value.Strip((byte)prefix, (byte)suffix);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> StripPrefixSuffixCharData() => new[]
    {
        OneByteChar, TwoByteChar, ThreeByteChar
    }.Permute2().FlatMap(StripData);

    [Theory, MemberData(nameof(StripPrefixSuffixCharData))]
    public void StripPrefixSuffixChar_ReturnsCorrectValue(
        U8String value,
        U8String expected,
        char prefix,
        char suffix)
    {
        var actual = value.Strip(prefix, suffix);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> StripPrefixSuffixRuneData() => new[]
    {
        OneByteRune, TwoByteRune, ThreeByteRune, FourByteRune
    }.Permute2().FlatMap(StripData);

    [Theory, MemberData(nameof(StripPrefixSuffixRuneData))]
    public void StripPrefixSuffixRune_ReturnsCorrectValue(
        U8String value,
        U8String expected,
        Rune prefix,
        Rune suffix)
    {
        var actual = value.Strip(prefix, suffix);

        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> StripPrefixSuffixStringData() => new[]
    {
        u8(Empty), u8(Latin), u8(Cyrillic), u8(Japanese), u8(Emoji), u8(Mixed)
    }.Permute2().FlatMap(StripData);

    [Theory, MemberData(nameof(StripPrefixSuffixStringData))]
    public void StripPrefixSuffixString_ReturnsCorrectValue(
        U8String value,
        U8String expected,
        U8String prefix,
        U8String suffix)
    {
        var overloads = new[]
        {
            value.Strip(prefix, suffix),
            value.Strip(prefix.AsSpan(), suffix.AsSpan())
        };

        foreach (var actual in overloads)
        {
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void StripPrefixSuffixString_ThrowsOnInvalidUtf8()
    {
        Assert.Throws<FormatException>(() => U8String.Empty.Strip(Empty, Invalid));
        Assert.Throws<FormatException>(() => U8String.Empty.Strip(Invalid, Empty));
        Assert.Throws<FormatException>(() => U8String.Empty.Strip(Invalid, Invalid));
    }
}