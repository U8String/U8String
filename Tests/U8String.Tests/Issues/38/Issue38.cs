namespace U8.Tests.Issues;

public class Issue38
{
    public static IEnumerable<object[]> Values()
    {
        var ascii = u8("Hello, World!");
        foreach (var c in Constants.AsciiWhitespace)
        {
            yield return [ascii, c + ascii];
            yield return [ascii, ascii + c];
            yield return [ascii, c + ascii + c];
        }

        var cyrilic = u8("Привіт, Всевіт!");
        foreach (var c in Constants.AsciiWhitespace)
        {
            yield return [cyrilic, c + cyrilic];
            yield return [cyrilic, cyrilic + c];
            yield return [cyrilic, c + cyrilic + c];
        }
    }

    [Theory, MemberData(nameof(Values))]
    public void TrimAscii_TrimsBothSides(U8String expected, U8String value)
    {
        var actual = value.TrimAscii();
        Assert.Equal(expected, actual);
    }
}