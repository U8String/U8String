namespace U8Primitives.Tests;

public class U8InfoTests
{
    [Fact]
    public void IsAsciiByte_TrueForAsciiBytes()
    {
        foreach (var b in TestConstants.AsciiBytes)
        {
            Assert.True(U8Info.IsAsciiByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsAsciiByte_FalseForNonAsciiBytes()
    {
        foreach (var b in TestConstants.NonAsciiBytes)
        {
            Assert.False(U8Info.IsAsciiByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsWhitespaceByte_TrueForWhitespaceBytes()
    {
        foreach (var b in TestConstants.AsciiWhitespaceBytes)
        {
            Assert.True(U8Info.IsAsciiWhitespace(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsAsciiWhitespace_FalseForNonWhitespaceBytes()
    {
        foreach (var b in Enumerable
            .Range(0, 256)
            .Select(i => (byte)i)
            .Except(TestConstants.AsciiWhitespaceBytes.ToArray()))
        {
            Assert.False(U8Info.IsAsciiWhitespace(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsContinuationByte_TrueForContinuationBytes()
    {
        foreach (var b in TestConstants.ContinuationBytes)
        {
            Assert.True(U8Info.IsContinuationByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsContinuationByte_FalseForNonContinuationBytes()
    {
        foreach (var b in TestConstants.NonContinuationBytes)
        {
            Assert.False(U8Info.IsContinuationByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void CharLength_IsOneForAsciiBytes()
    {
        foreach (var b in TestConstants.AsciiBytes)
        {
            Assert.Equal(1, U8Info.CharLength(b));
        }
    }

    [Fact]
    public void CharLength_IsTwoForCyrilicBytes()
    {
        foreach (var b in TestConstants.CyrilicCharBytes
            .Select(letter => letter[0]))
        {
            Assert.Equal(2, U8Info.CharLength(b));
        }
    }

    [Fact]
    public void CharLength_IsThreeForKanaBytes()
    {
        foreach (var b in TestConstants.KanaCharBytes
            .Select(letter => letter[0]))
        {
            Assert.Equal(3, U8Info.CharLength(b));
        }
    }

    [Fact]
    public void CharLength_IsFourForEmojiBytes()
    {
        foreach (var b in TestConstants.NonSurrogateEmojiChars
            .Select(letter => letter[0]))
        {
            Assert.Equal(4, U8Info.CharLength(b));
        }
    }
}