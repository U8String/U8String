using System.Text;

namespace U8.Tests.CaseConversion;

public class InvariantCaseConverterTests
{
    public static IEnumerable<object[]> ValidStrings => Constants.ValidStrings
        .Append(new ReferenceText(
            Name: "ASCIIRepeated",
            Utf16: Constants.Ascii + Constants.Ascii,
            Utf8: [.. Constants.AsciiBytes, .. Constants.AsciiBytes],
            Runes: [.. Constants.Ascii.EnumerateRunes(), .. Constants.Ascii.EnumerateRunes()]))
        .Append(new ReferenceText(
            Name: "MixedRepeated",
            Utf16: Constants.Mixed + Constants.Mixed,
            Utf8: [.. Constants.MixedBytes, .. Constants.MixedBytes],
            Runes: [.. Constants.Mixed.EnumerateRunes(), .. Constants.Mixed.EnumerateRunes()]))
        .Append(new ReferenceText(
            Name: "LowerThenUpper",
            Utf16: "їїЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇ",
            Utf8: [.. "їїЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇ"u8],
            Runes: [.. "їїЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇ".EnumerateRunes()]))
        .Append(new ReferenceText(
            Name: "UpperThenLower",
            Utf16: "ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї",
            Utf8: [.. "ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї"u8],
            Runes: [.. "ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї".EnumerateRunes()]))
        .Select(s => new[] { s });

    [Theory, MemberData(nameof(ValidStrings))]
    public void ToLower_ReturnsCorrectValue(ReferenceText text)
    {
        var lowercase = text.Utf16
            .Select(char.ToLowerInvariant)
            .ToArray();
        var source = new U8String(text.Utf8)
            .NullTerminate()[..^1];

        var expected = new U8String(lowercase);
        var actual = source.ToLower(U8CaseConversion.Invariant);

        Assert.Equal(expected, actual);
        Assert.True(actual.Equals(expected));
        Assert.True(actual.IsEmpty || actual.IsNullTerminated);
    }

    [Fact]
    public void ToLower_ConvertsMixedPatternShortLengthsCorrectly()
    {
        foreach (var pattern in Constants.MixedRunePatterns())
        {
            var lower = pattern.Select(Rune.ToLowerInvariant);
            var expected = U8String.Concat(lower);

            var actual = U8String.Concat(pattern).ToLower(U8CaseConversion.Invariant);

            Assert.Equal(expected, actual);
            Assert.Equal(!actual.IsEmpty, actual.IsNullTerminated);
        }
    }

    [Fact]
    public void ToLower_ThrowsOnToLowerFixedLength()
    {
        Assert.Throws<NotSupportedException>(() => U8CaseConversion.Invariant.ToLower([42], [0]));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void ToUpper_ReturnsCorrectValue(ReferenceText text)
    {
        var uppercase = text.Utf16
            .Select(char.ToUpperInvariant)
            .ToArray();
        var source = new U8String(text.Utf8)
            .NullTerminate()[..^1];

        var expected = new U8String(uppercase);
        var actual = source.ToUpper(U8CaseConversion.Invariant);

        Assert.Equal(expected, actual);
        Assert.True(actual.Equals(expected));
        Assert.True(actual.IsEmpty || actual.IsNullTerminated);
    }

    [Fact]
    public void ToUpper_ConvertsMixedPatternShortLengthsCorrectly()
    {
        foreach (var pattern in Constants.MixedRunePatterns())
        {
            var upper = pattern.Select(Rune.ToUpperInvariant);
            var expected = U8String.Concat(upper);

            var actual = U8String.Concat(pattern).ToUpper(U8CaseConversion.Invariant);

            Assert.Equal(expected, actual);
            Assert.Equal(!actual.IsEmpty, actual.IsNullTerminated);
        }
    }

    [Fact]
    public void ToUpper_ThrowsOnToUpperFixedLength()
    {
        Assert.Throws<NotSupportedException>(() => U8CaseConversion.Invariant.ToUpper([42], [0]));
    }
}
