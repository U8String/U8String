namespace U8.Tests.U8StringTests;

public partial class Manipulation
{
    [Fact]
    public void ReplaceLineEndings_ReturnsSourceWhenNothingToReplace()
    {
        var source = (U8String)"Hello, World!"u8;

        IEnumerable<U8String> Variants()
        {
            yield return source.ReplaceLineEndings();
            yield return source.ReplaceLineEndings(""u8);
            yield return source.ReplaceLineEndings("\0"u8);
            yield return source.ReplaceLineEndings("\n"u8);
            yield return source.ReplaceLineEndings("\r\n"u8);
            yield return source.ReplaceLineEndings("\t\r\n"u8);
        }

        foreach (var replaced in Variants())
        {
            Assert.Equal(source, replaced);
            Assert.True(replaced.SourceEqual(source));
            Assert.True(replaced.IsNullTerminated);
        }
    }
}
