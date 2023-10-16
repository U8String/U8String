namespace U8Primitives.Tests;

public class U8InfoTests
{
    [Fact]
    public void IsAsciiByte_TrueForAsciiBytes()
    {
        foreach (var b in Constants.AsciiBytes)
        {
            Assert.True(U8Info.IsAsciiByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsAsciiByte_FalseForNonAsciiBytes()
    {
        foreach (var b in Constants.NonAsciiBytes)
        {
            Assert.False(U8Info.IsAsciiByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsAsciiLetter_TrueForAsciiLetters()
    {
        foreach (var l in Constants.AsciiLetters)
        {
            Assert.True(U8Info.IsAsciiLetter(l), $"{(char)l}");
        }
    }

    [Fact]
    public void IsAsciiLetter_FalseForNonAsciiLetters()
    {
        foreach (var b in Constants.AsciiBytes.Except(Constants.AsciiLetters))
        {
            Assert.False(U8Info.IsAsciiLetter(b), $"{(char)b}");
        }
    }

    [Fact]
    public void IsWhitespaceByte_TrueForWhitespaceBytes()
    {
        foreach (var b in Constants.AsciiWhitespaceBytes)
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
            .Except(Constants.AsciiWhitespaceBytes.ToArray()))
        {
            Assert.False(U8Info.IsAsciiWhitespace(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsContinuationByte_TrueForContinuationBytes()
    {
        foreach (var b in Constants.ContinuationBytes)
        {
            Assert.True(U8Info.IsContinuationByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void IsWhitespaceRune_TrueForWhitespaceRunes()
    {
        foreach (var rune in Constants.WhitespaceRunes)
        {
            var utf8 = rune.ToUtf8();
            var message = $"Rune: 0x{rune.Value:X4} Bytes: {string.Join(" ", utf8.Select(b => b.ToString("X2")))}";

            Assert.True(U8Info.IsWhitespaceRune(utf8), message);
            Assert.True(U8Info.IsWhitespaceRune(ref utf8[0], out var length), message);
            Assert.Equal(rune.Utf8SequenceLength, length);
            Assert.Equal(rune.Utf8SequenceLength, utf8.Length);
        }
    }

    [Fact]
    public void IsWhitespaceRune_FalseForNonWhitespaceRunes()
    {
        // Exhaustive evaluation
        foreach (var rune in Constants.NonWhitespaceRunes)
        {
            var utf8 = rune.ToUtf8();
            var message = $"Rune: 0x{rune.Value:X4} Bytes: {string.Join(" ", utf8.Select(b => b.ToString("X2")))}";
            
            Assert.False(U8Info.IsWhitespaceRune(utf8), message);
            Assert.False(U8Info.IsWhitespaceRune(ref utf8[0], out var length), message);
            Assert.Equal(rune.Utf8SequenceLength, length);
            Assert.Equal(rune.Utf8SequenceLength, utf8.Length);
        }
    }

    [Fact]
    public void IsContinuationByte_FalseForNonContinuationBytes()
    {
        foreach (var b in Constants.NonContinuationBytes)
        {
            Assert.False(U8Info.IsContinuationByte(b), $"0x{b:X2}");
        }
    }

    [Fact]
    public void CharLength_IsOneForAsciiBytes()
    {
        foreach (var b in Constants.AsciiBytes)
        {
            Assert.Equal(1, U8Info.RuneLength(b));
        }
    }

    [Fact]
    public void CharLength_IsTwoForCyrilicBytes()
    {
        foreach (var b in Constants.CyrilicCharBytes
            .Select(letter => letter[0]))
        {
            Assert.Equal(2, U8Info.RuneLength(b));
        }
    }

    [Fact]
    public void CharLength_IsThreeForKanaBytes()
    {
        foreach (var b in Constants.KanaCharBytes
            .Select(letter => letter[0]))
        {
            Assert.Equal(3, U8Info.RuneLength(b));
        }
    }

    [Fact]
    public void CharLength_IsFourForEmojiBytes()
    {
        foreach (var b in Constants.NonSurrogateEmojiChars
            .Select(letter => letter[0]))
        {
            Assert.Equal(4, U8Info.RuneLength(b));
        }
    }
}
