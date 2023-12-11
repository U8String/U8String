using U8.Comparison;

namespace U8.Tests.U8ComparisonTests;

public class AsciiIgnoreCaseConverter
{
    [Fact]
    public void CommonPrefixLength_CalculatesCorrectLength()
    {
        // TODO: Cover more cases
        var left = "Hell."u8;
        var right = "HELLO"u8;
        var commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(4, commonPrefixLength);

        left = "Hello"u8;
        right = "HELLO"u8;
        commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(5, commonPrefixLength);

        left = "Hello, World! Hello, World!"u8;
        right = "HELLO, WORLD! HELLO, WORLD@"u8;
        commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(26, commonPrefixLength);

        left = "Hello, World! Hello, World!"u8;
        right = "HELLO, WORLD! HELLO, WORLD!"u8;
        commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(27, commonPrefixLength);

        left = "Hello, World! Hello, World! Hello, World! Hello, World!"u8;
        right = "HELLO, WORLD! HELLO, WORLD@ HELLO, WORLD! HELLO, WORLD!"u8;
        commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(26, commonPrefixLength);

        left = "Hello, World! Hello, World! Hello, World! Hello, World!"u8;
        right = "HELLO, WORLD! HELLO, WORLD! HELLO, WORLD! HELLO, WORLD!"u8;
        commonPrefixLength = U8AsciiIgnoreCaseComparer.CommonPrefixLength(left, right);
        Assert.Equal(55, commonPrefixLength);
    }
}
