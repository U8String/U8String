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
            Utf8: [.."їїЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇ"u8],
            Runes: [.."їїЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇЇ".EnumerateRunes()]))
        .Append(new ReferenceText(
            Name: "UpperThenLower",
            Utf16: "ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї",
            Utf8: [.."ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї"u8],
            Runes: [.."ЇЇїїїїїїїїїїїїїїїїїїїїїїїїїї".EnumerateRunes()]))
        .Select(s => new[] { s });

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToLowerReturnsCorrectValue(ReferenceText text)
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

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToUpperReturnsCorrectValue(ReferenceText text)
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
}
