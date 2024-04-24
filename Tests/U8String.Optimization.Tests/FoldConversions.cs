using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт, Всесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodLiteralDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            "Привіт, Всесвіт!".ToU8String(),
            "Привіт, Всесвіт!".ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт, Всесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
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
            U8String.FromAscii(OneThousandRunes)
        };

        var first = literals[0];
        var reference = Encoding.UTF8.GetBytes(OneThousandRunes);

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal(reference.AsSpan(), literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodReferencedConstDeclaration1KRunes_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            OneThousandRunes.ToU8String(),
            OneThousandRunes.ToU8String()
        };

        var first = literals[0];
        var reference = Encoding.UTF8.GetBytes(OneThousandRunes);

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal(reference.AsSpan(), literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
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

        // Windows may have inconsistent line endings depending on project settings,
        // we don't particularly care for those and users are expected to call .ReplaceLineEndings(lineEnding)
        // if they need to normalize the text for a specific platform or protocol.
        if (OperatingSystem.IsWindows())
        {
            literals = literals[..2];
        }

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт,\nВсесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodUtf8LiteralExpression_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            "Привіт,\nВсесвіт!"u8.ToU8String(),
            "Привіт,\nВсесвіт!"u8.ToU8String(),
            """
                Привіт,
                Всесвіт!
                """u8.ToU8String()
        };

        // Windows may have inconsistent line endings depending on project settings,
        // we don't particularly care for those and users are expected to call .ReplaceLineEndings(lineEnding)
        // if they need to normalize the text for a specific platform or protocol.
        if (OperatingSystem.IsWindows())
        {
            literals = literals[..1];
        }

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Привіт,\nВсесвіт!"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineBoolConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(true),
            U8String.Create(true)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("True"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineByteConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8((byte)255),
            u8(byte.MaxValue),
            U8String.Create(byte.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("255"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineByteConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            ((byte)255).ToU8String(),
            byte.MaxValue.ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("255"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineCharConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8('Ї'),
            U8String.Create('Ї')
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Ї"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineCharConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            'Ї'.ToU8String(),
            'Ї'.ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Ї"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineIntConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(int.MaxValue),
            U8String.Create(int.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("2147483647"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineIntConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            2147483647.ToU8String(),
            int.MaxValue.ToU8String(),
            int.MaxValue.ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("2147483647"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineLongConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(long.MaxValue),
            u8(9223372036854775807),
            U8String.Create(long.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("9223372036854775807"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineLongConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            9223372036854775807.ToU8String(),
            long.MaxValue.ToU8String(),
            long.MaxValue.ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("9223372036854775807"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineDecimalConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(1234567890.123456789M),
            u8(1234567890.123456789M),
            U8String.Create(1234567890.123456789M)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("1234567890.123456789"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineDecimalConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            1234567890.123456789M.ToU8String(),
            1234567890.123456789M.ToU8String(),
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("1234567890.123456789"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void InlineDecimalMaxValueConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            u8(decimal.MaxValue),
            u8(decimal.MaxValue),
            U8String.Create(decimal.MaxValue)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("79228162514264337593543950335"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ExtensionMethodInlineDecimalMaxValueConstantDeclaration_IsCorrectlyFolded()
    {
        var literals = new[]
        {
            decimal.MaxValue.ToU8String(),
            decimal.MaxValue.ToU8String(),
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("79228162514264337593543950335"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void NonContiguousDuplicateEnumDeclaration_IsCorrectlyFolded()
    {
        const HttpStatusCode reference = HttpStatusCode.Ambiguous;

        var literals = new[]
        {
            reference.ToU8String(),
            HttpStatusCode.MultipleChoices.ToU8String(), // :^)
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Ambiguous"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void ContiguousUniqueEnumDeclaration_IsCorrectlyFolded()
    {
        const DayOfWeek reference = DayOfWeek.Friday;

        var literals = new[]
        {
            reference.ToU8String(),
            DayOfWeek.Friday.ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("Friday"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void UndefinedEnumDeclaration_IsCorrectlyFolded()
    {
        const HttpStatusCode reference = (HttpStatusCode)int.MaxValue;

        var literals = new[]
        {
            reference.ToU8String(),
            ((HttpStatusCode)int.MaxValue).ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("2147483647"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void FlagsEnumDeclaration_IsCorrectlyFolded()
    {
        const RegexOptions reference = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;

        var literals = new[]
        {
            reference.ToU8String(),
            (RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture).ToU8String(),
            (RegexOptions.None | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture).ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("ExplicitCapture, Compiled, CultureInvariant"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
        }
    }

    [Fact]
    public void UndefinedFlagsEnumDeclaration_IsCorrectlyFolded()
    {
        const RegexOptions reference = (RegexOptions)int.MaxValue;

        var literals = new[]
        {
            reference.ToU8String(),
            ((RegexOptions)int.MaxValue).ToU8String()
        };

        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(first, literals[i]);
            Assert.Equal("2147483647"u8, literals[i]);
            Assert.True(first.Equals(literals[i]));
            Assert.True(first.SourceEqual(literals[i]));
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
        Assert.False(fromUtf8.SourceEqual(fromUtf16));
        Assert.NotEqual(fromUtf8.Source, fromUtf16.Source);
    }

    [Fact]
    public void ToU8String_DoesNotBreakTheBuildAndIsNotTriggeredOnNonConstantValuesWithConstantArguments()
    {
        var date = DateTime.Now;
        var literals = new[]
        {
            DateTime.Now.ToU8String("yyyy-MM-dd"),
            DateTime.Now.ToU8String("yyyy-MM-dd"),
            date.ToU8String("yyyy-MM-dd"),
        };

        var expected = date.ToString("yyyy-MM-dd").ToU8String();
        var first = literals[0];

        for (var i = 1; i < literals.Length; i++)
        {
            Assert.Equal(expected, literals[i]);
            Assert.True(expected.Equals(literals[i]));
            Assert.True(literals[i].IsNullTerminated);
            Assert.False(first.SourceEqual(literals[i]));
        }
    }
}
