namespace U8.Tests.CaseConversion;

public class AsciiCaseConverterTests
{
    public static IEnumerable<object[]> ValidStrings => Constants.ValidStrings.Select(s => new[] { s });

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToLowerReturnsCorrectValue(ReferenceText text)
    {
        var lowercase = text.Utf16
            .Select(c => char.IsAsciiLetter(c) ? (char)(c | 0x20) : c)
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

    [Theory, MemberData(nameof(ValidStrings))]
    public void AsciiCaseConverter_ToUpperReturnsCorrectValue(ReferenceText text)
    {
        var uppercase = text.Utf16
            .Select(c => char.IsAsciiLetter(c) ? (char)(c & ~0x20) : c)
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
}
