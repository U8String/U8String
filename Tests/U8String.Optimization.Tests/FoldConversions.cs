using System.Text;

namespace U8.Optimization.Tests;

// TODO: Implement rest of the coverage.
public partial class FoldConversions
{
    [Fact]
    public void InlineLiteralDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8("Привіт, Всесвіт!"),
            U8String.Create("Привіт, Всесвіт!"),
            U8String.CreateLossy("Привіт, Всесвіт!"),
            // TODO: Folding prevents this from throwing which is not correct. Just remove?
            U8String.FromAscii("Привіт, Всесвіт!"),
            U8String.FromLiteral("Привіт, Всесвіт!")
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт, Всесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ReferencedConstDeclaration1KRunes_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(OneThousandRunes),
            U8String.Create(OneThousandRunes),
            U8String.CreateLossy(OneThousandRunes),
            U8String.FromAscii(OneThousandRunes),
            U8String.FromLiteral(OneThousandRunes)
        };

        var first = literals[0];
        var reference = Encoding.UTF8.GetBytes(OneThousandRunes);

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal(reference.AsSpan(), literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void Utf8LiteralExpression_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8("Привіт,\nВсесвіт!"u8),
            U8String.Create("Привіт,\nВсесвіт!"u8),
            u8("""
                Привіт,
                Всесвіт!
                """u8),
            U8String.Create("""
                Привіт,
                Всесвіт!
                """u8)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт,\nВсесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineBoolConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(true), U8String.Create(true)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("True"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineByteConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(byte.MaxValue), U8String.Create(byte.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("255"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineIntConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(int.MaxValue), U8String.Create(int.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("2147483647"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineLongConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(long.MaxValue), U8String.Create(long.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("9223372036854775807"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineDecimalConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(1234567890.123456789M), U8String.Create(1234567890.123456789M)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("1234567890.123456789"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineDecimalMaxValueConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(decimal.MaxValue), U8String.Create(decimal.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("79228162514264337593543950335"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    // This implmenetation detail might change in the future should FoldConversion be improved
    // to store literal values and associated interceptors separately to further deduplicate
    // identical text, but for now, this is the expected behavior.
    [Fact]
    public void IdenticalUtf16Utf8Literals_BuildSuccessfullyAndHaveDifferentSource()
    {
        var fromUtf8 = u8("Hello, World!"u8);
        var fromUtf16 = u8("Hello, World!");

        Assert.Equal(fromUtf8, fromUtf16);
        Assert.False(fromUtf8.SourceEquals(fromUtf16));
        Assert.NotEqual(fromUtf8.Source, fromUtf16.Source);
    }
}
