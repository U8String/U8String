using System.Text;

namespace U8.Tools.Tests.Generators;

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
            U8String.CreateInterned("Привіт, Всесвіт!"),
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
            U8String.CreateInterned(OneThousandRunes),
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
    public void ReferencedConstDeclaration1MRunes_IsCorrectlyFolded()
    {
       var literals = new[]
       {
           u8(OneMillionRunes),
           U8String.Create(OneMillionRunes),
           U8String.CreateLossy(OneMillionRunes),
           U8String.CreateInterned(OneMillionRunes),
           U8String.FromAscii(OneMillionRunes),
           U8String.FromLiteral(OneMillionRunes)
       };

       var first = literals[0];
       var reference = Encoding.UTF8.GetBytes(OneMillionRunes);

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
    public void InlineFloatConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(float.MaxValue), U8String.Create(float.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("3.4028235E+38"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEquals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineDoubleConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(double.MaxValue), U8String.Create(double.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("1.7976931348623157E+308"u8, literals[i]);
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
}