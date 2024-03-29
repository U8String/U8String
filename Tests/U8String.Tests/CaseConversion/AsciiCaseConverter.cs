using U8.CaseConversion;

namespace U8.Tests.CaseConversion;

public class AsciiCaseConverterTests
{
    public static IEnumerable<object[]> ValidStrings => Constants.ValidStrings
        .Append(new ReferenceText(
            Name: "ASCIIRepeated",
            Utf16: Constants.Ascii + Constants.Ascii,
            Utf8: [..Constants.AsciiBytes, ..Constants.AsciiBytes],
            Runes: [..Constants.Ascii.EnumerateRunes(), ..Constants.Ascii.EnumerateRunes()]))
        .Append(new ReferenceText(
            Name: "MixedRepeated",
            Utf16: Constants.Mixed + Constants.Mixed,
            Utf8: [..Constants.MixedBytes, ..Constants.MixedBytes],
            Runes: [..Constants.Mixed.EnumerateRunes(), ..Constants.Mixed.EnumerateRunes()]))
        .Select(s => new[] { s });

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToLowerReturnsCorrectValue(ReferenceText text)
    {
        var lowercase = text.Utf16
            .Select(c => char.IsAsciiLetter(c) ? char.ToLowerInvariant(c) : c)
            .ToArray();
        var source = new U8String(text.Utf8)
            .NullTerminate()[..^1];

        var expected = new U8String(lowercase);
        var actual = new[]
        {
            source.ToLowerAscii(),
            source.ToLower(U8CaseConversion.Ascii),
        };

        foreach (var result in actual)
        {
            Assert.Equal(expected, result);
            Assert.True(result.Equals(expected));
            Assert.True(result.IsEmpty || result.IsNullTerminated);
        }
    }

    [Fact]
    public void AsciiCaseConverter_ToLowerThrowsOnTooShortDestination()
    {
        var source = (U8String)"Hello, World!"u8;

        Assert.Throws<ArgumentException>(() => U8AsciiCaseConverter.Instance.ToLower(source, []));
        Assert.Throws<ArgumentException>(() => U8AsciiCaseConverter.Instance.ToLower(source, new byte[source.Length - 1]));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToUpperReturnsCorrectValue(ReferenceText text)
    {
        var uppercase = text.Utf16
            .Select(c => char.IsAsciiLetter(c) ? char.ToUpperInvariant(c) : c)
            .ToArray();
        var source = new U8String(text.Utf8)
            .NullTerminate()[..^1];

        var expected = new U8String(uppercase);
        var actual = new[]
        {
            source.ToUpperAscii(),
            source.ToUpper(U8CaseConversion.Ascii),
        };

        foreach (var result in actual)
        {
            Assert.Equal(expected, result);
            Assert.True(result.Equals(expected));
            Assert.True(result.IsEmpty || result.IsNullTerminated);
        }
    }

    [Fact]
    public void AsciiCaseConverter_ToUpperThrowsOnTooShortDestination()
    {
        var source = (U8String)"Hello, World!"u8;

        Assert.Throws<ArgumentException>(() => U8AsciiCaseConverter.Instance.ToUpper(source, []));
        Assert.Throws<ArgumentException>(() => U8AsciiCaseConverter.Instance.ToUpper(source, new byte[source.Length - 1]));
    }
}
