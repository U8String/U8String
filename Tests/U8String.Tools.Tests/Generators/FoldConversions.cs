namespace U8.Tools.Tests.Generators;

// TODO: Implement rest of the coverage.
public class FoldConversions
{
    [Fact]
    public void InlineLiteralDeclaration_IsCorrectlyFolded()
    {
        var list = new List<U8String>();

        for (var i = 0; i < 3; i++)
        {
            list.Add(u8("Привіт, Всесвіт!"));
        }

        var first = list[0];

        for (var i = 1; i < list.Count; i++)
        {
            Assert.Equal(first, list[i]);
            Assert.Equal("Привіт, Всесвіт!"u8, list[i]);
            Assert.True(first.Equals(list[i]));
            Assert.True(first.SourceEquals(list[i]));
            Assert.True(list[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineBoolConstantDeclaration_IsCorrectlyFolded()
    {
        var list = new List<U8String>();

        for (var i = 0; i < 3; i++)
        {
            list.Add(u8(true));
        }

        var first = list[0];

        for (var i = 1; i < list.Count; i++)
        {
            Assert.Equal(first, list[i]);
            Assert.Equal("True"u8, list[i]);
            Assert.True(first.Equals(list[i]));
            Assert.True(first.SourceEquals(list[i]));
            Assert.True(list[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineByteConstantDeclaration_IsCorrectlyFolded()
    {
        var list = new List<U8String>();

        for (var i = 0; i < 3; i++)
        {
            list.Add(u8(byte.MaxValue));
        }

        var first = list[0];

        for (var i = 1; i < list.Count; i++)
        {
            Assert.Equal(first, list[i]);
            Assert.Equal("255"u8, list[i]);
            Assert.True(first.Equals(list[i]));
            Assert.True(first.SourceEquals(list[i]));
            Assert.True(list[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineIntConstantDeclaration_IsCorrectlyFolded()
    {
        var list = new List<U8String>();

        for (var i = 0; i < 3; i++)
        {
            list.Add(u8(999));
        }

        var first = list[0];

        for (var i = 1; i < list.Count; i++)
        {
            Assert.Equal(first, list[i]);
            Assert.Equal("999"u8, list[i]);
            Assert.True(first.Equals(list[i]));
            Assert.True(first.SourceEquals(list[i]));
            Assert.True(list[i].IsNullTerminated);
        }
    }
}
