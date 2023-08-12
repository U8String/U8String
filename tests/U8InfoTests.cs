namespace U8Primitives.Tests;

public class U8InfoTests
{
    [Fact]
    public void IsContinuationByte()
    {
        Assert.True(U8Info.IsContinuationByte(0b1000_0000));
        Assert.True(U8Info.IsContinuationByte(0b1011_1111));
        Assert.False(U8Info.IsContinuationByte(0b0000_0000));
        Assert.False(U8Info.IsContinuationByte(0b0111_1111));
    }

    [Fact]
    public void IsAsciiByte()
    {
        Assert.True(U8Info.IsAsciiByte(0b0000_0000));
        Assert.True(U8Info.IsAsciiByte(0b0111_1111));
        Assert.False(U8Info.IsAsciiByte(0b1000_0000));
        Assert.False(U8Info.IsAsciiByte(0b1011_1111));
    }

    [Fact]
    public void CodepointLength()
    {
        Assert.Equal(0, U8Info.CodepointLength(0b1000_0000));
        Assert.Equal(0, U8Info.CodepointLength(0b1011_1111));
        Assert.Equal(1, U8Info.CodepointLength(0b0000_0000));
        Assert.Equal(1, U8Info.CodepointLength(0b0111_1111));
        Assert.Equal(2, U8Info.CodepointLength(0b1100_0000));
        Assert.Equal(2, U8Info.CodepointLength(0b1101_1111));
        Assert.Equal(3, U8Info.CodepointLength(0b1110_0000));
        Assert.Equal(3, U8Info.CodepointLength(0b1110_1111));
        Assert.Equal(4, U8Info.CodepointLength(0b1111_0000));
        Assert.Equal(4, U8Info.CodepointLength(0b1111_0111));
    }

    [Fact]
    public void IsAsciiWhitespace()
    {
        Assert.True(U8Info.IsAsciiWhitespace(0x20));
        Assert.True(U8Info.IsAsciiWhitespace(0x09));
        Assert.True(U8Info.IsAsciiWhitespace(0x0A));
        Assert.True(U8Info.IsAsciiWhitespace(0x0B));
        Assert.True(U8Info.IsAsciiWhitespace(0x0C));
        Assert.True(U8Info.IsAsciiWhitespace(0x0D));
    }
}