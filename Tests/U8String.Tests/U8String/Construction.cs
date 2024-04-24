using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

using U8.CompilerServices;
using U8.IO;
using U8.Primitives;

namespace U8.Tests.U8StringTests;

public class Construction
{
    public static readonly IEnumerable<object[]> ValidStrings = Constants.ValidStrings.Select(s => new[] { s });

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorByteArray_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(text.Utf8);
        if (bytes is []) return;
        Assert.IsType<byte[]>(bytes);

        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.ToU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(bytes.Length, str.Length);
            // Assumes text is not null-terminated
            Assert.Equal(bytes.Length + 1, str._value!.Length);
            Assert.NotEqual(bytes, str._value);
            Assert.True(str.Equals(bytes));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteArray_ProducesCorrectResultWhenEmpty()
    {
        byte[] bytes = [];

        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.ToU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteArray_ThrowsOnNullReference()
    {
        var bytes = (byte[]?)null;

        Assert.Throws<ArgumentNullException>(() => u8(bytes!));
        Assert.Throws<ArgumentNullException>(() => (U8String)bytes!);
        Assert.Throws<ArgumentNullException>(() => bytes!.ToU8String());
        Assert.Throws<ArgumentNullException>(() => new U8String(bytes!));
        Assert.Throws<ArgumentNullException>(() => U8String.Create(bytes!));
        Assert.False(U8String.TryCreate(bytes, out _));
    }

    [Fact]
    public void CtorByteArray_ThrowsOnInvalidUtf8()
    {
        byte[] bytes = [0x80, 0x80, 0x80, 0x80];

        Assert.Throws<FormatException>(() => u8(bytes));
        Assert.Throws<FormatException>(() => (U8String)bytes);
        Assert.Throws<FormatException>(() => bytes.ToU8String());
        Assert.Throws<FormatException>(() => new U8String(bytes));
        Assert.Throws<FormatException>(() => U8String.Create(bytes));
        Assert.False(U8String.TryCreate(bytes, out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorByteSpan_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8.AsSpan();
        if (bytes is []) return;

        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.ToU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.Utf8SpanParsableParse<U8String>(bytes),
            Extensions.Utf8SpanParsableTryParse<U8String>(bytes, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(bytes.Length, str.Length);
            // Assumes text is not null-terminated
            Assert.Equal(bytes.Length + 1, str._value!.Length);
            Assert.True(str.Equals(bytes));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteSpan_ProducesCorrectResultWhenEmpty()
    {
        ReadOnlySpan<byte> bytes = [];

        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.ToU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.Utf8SpanParsableParse<U8String>(bytes),
            Extensions.Utf8SpanParsableTryParse<U8String>(bytes, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteSpan_ThrowsOnInvalidUtf8()
    {
        byte[] bytes = [0x80, 0x80, 0x80, 0x80];

        Assert.Throws<FormatException>(() => u8(bytes.AsSpan()));
        Assert.Throws<FormatException>(() => bytes.AsSpan().ToU8String());
        Assert.Throws<FormatException>(() => new U8String(bytes.AsSpan()));
        Assert.Throws<FormatException>(() => U8String.Create(bytes.AsSpan()));
        Assert.Throws<FormatException>(() => (U8String)(ReadOnlySpan<byte>)bytes);
        Assert.Throws<FormatException>(() => Extensions.Utf8SpanParsableParse<U8String>(bytes.AsSpan()));
        Assert.False(U8String.TryCreate(bytes.AsSpan(), out _));
        Assert.False(Extensions.Utf8SpanParsableTryParse<U8String>(bytes.AsSpan(), out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorImmutableArray_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8;
        if (bytes is []) return;
        Assert.IsType<ImmutableArray<byte>>(bytes);

        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.AsU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.True(str.Equals(bytes));
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(bytes.Length, str.Length);

            // Ensure the ctor is no-copy
            Assert.Same(ImmutableCollectionsMarshal.AsArray(bytes), str._value);
        }
    }

    public static IEnumerable<object[]> EmptyImmutableArrays()
    {
        yield return [ImmutableArray<byte>.Empty];
        yield return [ImmutableCollectionsMarshal.AsImmutableArray<byte>(null)];
        yield return [ImmutableCollectionsMarshal.AsImmutableArray(Array.Empty<byte>())];
    }

    [Theory, MemberData(nameof(EmptyImmutableArrays))]
    public void CtorImmutableArray_ProducesCorrectResultWhenEmpty(ImmutableArray<byte> bytes)
    {
        var actual = new[]
        {
            u8(bytes),
            (U8String)bytes,
            bytes.AsU8String(),
            new U8String(bytes),
            U8String.Create(bytes),
            U8String.TryCreate(bytes, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorImmutableArray_ThrowsOnInvalidUtf8()
    {
        ImmutableArray<byte> bytes = [0x80, 0x80, 0x80, 0x80];

        Assert.Throws<FormatException>(() => u8(bytes));
        Assert.Throws<FormatException>(() => (U8String)bytes);
        Assert.Throws<FormatException>(() => bytes.AsU8String());
        Assert.Throws<FormatException>(() => new U8String(bytes));
        Assert.Throws<FormatException>(() => U8String.Create(bytes));
        Assert.False(U8String.TryCreate(bytes, out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorString_ProducesCorrectResult(ReferenceText text)
    {
        var utf8 = text.Utf8;
        if (utf8 is []) return;
        Assert.IsType<string>(text.Utf16);

        var actual = new[]
        {
            u8(text.Utf16),
            (U8String)text.Utf16,
            text.Utf16.ToU8String(),
            new U8String(text.Utf16),
            U8String.Create(text.Utf16),
            U8String.TryCreate(text.Utf16, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.ParsableParse<U8String>(text.Utf16),
            Extensions.ParsableTryParse<U8String>(text.Utf16, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(utf8, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(utf8.Length, str.Length);
            Assert.Equal(utf8.Length + 1, str._value!.Length);
            Assert.True(str.Equals(utf8));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorString_ProducesCorrectResultWhenEmpty()
    {
        var empty = string.Empty;
        var bytes = Array.Empty<byte>();

        var actual = new[]
        {
            u8(empty),
            (U8String)empty,
            empty.ToU8String(),
            new U8String(empty),
            U8String.Create(empty),
            U8String.TryCreate(empty, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.ParsableParse<U8String>(empty),
            Extensions.ParsableTryParse<U8String>(empty, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorString_ThrowsOnNullReference()
    {
        const string? value = null;

        Assert.Throws<ArgumentNullException>(() => u8(value!));
        Assert.Throws<ArgumentNullException>(() => (U8String)value!);
        Assert.Throws<ArgumentNullException>(() => new U8String(value!));
        Assert.Throws<ArgumentNullException>(() => U8String.Create(value!));
        Assert.Throws<ArgumentNullException>(() => value!.ToU8String());
        Assert.Throws<ArgumentNullException>(() => Extensions.ParsableParse<U8String>(value!));
        Assert.False(U8String.TryCreate(value, out _));
        Assert.False(Extensions.ParsableTryParse<U8String>(value, out _));
    }

    public static TheoryData<string> TornSurrogates = new(
    [
        "🔥"[..^1],
        "🔥🔥"[..^1],
        "🔥🔥🔥"[..^1]
    ]);

    [Theory, MemberData(nameof(TornSurrogates))]
    public void CtorString_ThrowsOnTornSurrogatePair(string value)
    {
        Assert.Throws<FormatException>(() => u8(value));
        Assert.Throws<FormatException>(() => (U8String)value);
        Assert.Throws<FormatException>(() => new U8String(value));
        Assert.Throws<FormatException>(() => U8String.Create(value));
        Assert.Throws<FormatException>(() => value.ToU8String());
        Assert.Throws<FormatException>(() => Extensions.ParsableParse<U8String>(value));

        Assert.False(U8String.TryCreate(value, out _));
        Assert.False(Extensions.ParsableTryParse<U8String>(value, out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorCharSpan_ProducesCorrectResult(ReferenceText text)
    {
        var utf8 = text.Utf8;
        if (utf8 is []) return;

        ReadOnlySpan<char> chars = text.Utf16;

        var actual = new[]
        {
            u8(chars),
            (U8String)chars,
            chars.ToU8String(),
            new U8String(chars),
            U8String.Create(chars),
            U8String.TryCreate(chars, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.SpanParsableParse<U8String>(chars),
            Extensions.SpanParsableTryParse<U8String>(chars, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(utf8, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(utf8.Length, str.Length);
            Assert.Equal(utf8.Length + 1, str._value!.Length);
            Assert.True(str.Equals(utf8));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorCharSpan_ProducesCorrectResultWhenEmpty()
    {
        ReadOnlySpan<byte> bytes = [];
        ReadOnlySpan<char> chars = [];

        var actual = new[]
        {
            u8(chars),
            (U8String)chars,
            chars.ToU8String(),
            new U8String(chars),
            U8String.Create(chars),
            U8String.TryCreate(chars, out var s)
                ? s : ThrowHelpers.Unreachable<U8String>(),
            Extensions.SpanParsableParse<U8String>(chars),
            Extensions.SpanParsableTryParse<U8String>(chars, out var s2)
                ? s2 : ThrowHelpers.Unreachable<U8String>()
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }


    [Theory, MemberData(nameof(TornSurrogates))]
    public void CtorCharSpan_ThrowsOnTornSurrogatePair(string value)
    {
        Assert.Throws<FormatException>(() => u8(value.AsSpan()));
        Assert.Throws<FormatException>(() => (U8String)value.AsSpan());
        Assert.Throws<FormatException>(() => new U8String(value.AsSpan()));
        Assert.Throws<FormatException>(() => U8String.Create(value.AsSpan()));
        Assert.Throws<FormatException>(() => value.AsSpan().ToU8String());
        Assert.Throws<FormatException>(() => Extensions.SpanParsableParse<U8String>(value.AsSpan()));

        Assert.False(U8String.TryCreate(value.AsSpan(), out _));
        Assert.False(Extensions.SpanParsableTryParse<U8String>(value.AsSpan(), out _));
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorByteSpanUnchecked_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8.AsSpan();
        if (bytes is []) return;

        var actual = new[]
        {
            U8Unchecked.Create(bytes),
            new U8String(bytes, skipValidation: true)
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(bytes.Length, str.Length);
            Assert.Equal(bytes.Length + 1, str._value!.Length);
            Assert.True(str.Equals(bytes));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteSpanUnchecked_ProducesCorrectResultWhenEmpty()
    {
        ReadOnlySpan<byte> bytes = [];

        var actual = new[]
        {
            U8Unchecked.Create(bytes),
            new U8String(bytes, skipValidation: true)
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CtorByteSpanUnchecked_DoesNotThrowOnInvalidUtf8()
    {
        byte[] bytes = [0x80, 0x80, 0x80, 0x80];

        _ = U8Unchecked.Create(bytes);
        _ = new U8String(bytes, skipValidation: true);
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public unsafe void CtorNullTerminatedPointer_ProducesCorrectResult(ReferenceText text)
    {
        if (text.Utf8 is []) return;

        var bytes = (byte[])[.. text.Utf8, 0];
        if (bytes is [0, ..])
            bytes = bytes[1..];

        fixed (byte* ptr = bytes)
        {
            bytes = bytes[..^1];

            var actual = new[]
            {
                new U8String(ptr),
                U8String.Create(ptr)
            };

            foreach (var str in actual)
            {
                Assert.Equal(bytes, str);
                Assert.Equal(0, str.Offset);
                Assert.Equal(bytes.Length, str.Length);
                Assert.Equal(bytes.Length + 1, str._value!.Length);
                Assert.True(str.Equals(bytes));
                Assert.True(str.IsNullTerminated);
            }
        }
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorArrayIntInt_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8.ToArray();
        if (bytes is []) return;

        foreach (var offset in (Span<int>)[0, 1, 3, 7])
        {
            var length = bytes.Length - offset;
            var str = new U8String(bytes, offset, length);

            Assert.Equal(offset, str.Offset);
            Assert.Equal(length, str.Length);
            Assert.Same(bytes, str._value);
        }
    }

    [Fact]
    public void CtorArrayIntInt_ProducesCorrectResultWhenLengthIsZero()
    {
        var bytes = new byte[7];

        foreach (var offset in (Span<int>)[0, 1, 3, 7])
        {
            var str = new U8String(bytes, offset, 0);

            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.Equal(offset, str.Offset);
            Assert.Equal(0, str.Length);
        }
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CtorArrayRange_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8.ToArray();
        if (bytes is []) return;

        foreach (var offset in (Span<int>)[0, 1, 3, 7])
        {
            var length = bytes.Length - offset;
            var range = new U8Range(offset, length);
            var str = new U8String(bytes, range);

            Assert.Equal(offset, str.Offset);
            Assert.Equal(length, str.Length);
            Assert.Equal(range, str.Range);
            Assert.Same(bytes, str._value);
        }
    }

    [Fact]
    public void CtorArrayRange_ProducesCorrectResultWhenLengthIsZero()
    {
        var bytes = new byte[7];
        foreach (var offset in (Span<int>)[0, 1, 3, 7])
        {
            var range = new U8Range(offset, 0);
            var str = new U8String(bytes, range);

            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.Equal(offset, str.Offset);
            Assert.Equal(0, str.Length);
        }
    }

    [Theory, InlineData(true), InlineData(false)]
    public void CreateFromBool_ProducesCorrectResult(bool value)
    {
        var actual = new[]
        {
            value.ToU8String(),
            U8String.Create(value)
        };

        foreach (var str in actual)
        {
            var expected = Encoding.UTF8.GetBytes(value.ToString());

            Assert.Equal(expected, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(expected.Length, str.Length);
            Assert.True(str.Equals(expected));
            Assert.True(str.IsNullTerminated);
            // Ensure it is always a cached literal
            Assert.True(str.SourceEqual(value.ToU8String()));
            Assert.True(str.SourceEqual(U8String.Create(value)));
        }
    }

    [Fact]
    public void CreateFromByte_ProducesCorrectResult()
    {
        for (var i = 0; i <= byte.MaxValue; i++)
        {
            var b = (byte)i;
            var expected = Encoding.UTF8.GetBytes(b.ToString());
            var actual = new[]
            {
                b.ToU8String(),
                U8String.Create(b)
            };

            foreach (var str in actual)
            {
                var msg = $"Expected {b}. Got: {actual}";
                Assert.Equal(expected, str);
                Assert.Equal(0, str.Offset);
                Assert.Equal(expected.Length, str.Length);
                Assert.True(str.Equals(expected), msg);
                Assert.True(str.IsNullTerminated);
                // Ensure it is always a cached literal
                Assert.True(str.SourceEqual(b.ToU8String()));
                Assert.True(str.SourceEqual(U8String.Create(b)));
            }
        }
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CreateFromImmutableArrayUnchecked_ProducesCorrectResult(ReferenceText text)
    {
        var bytes = text.Utf8;
        if (bytes is []) return;
        Assert.IsType<ImmutableArray<byte>>(bytes);

        var actual = U8Unchecked.Create(bytes);

        Assert.True(actual.Equals(bytes));
        Assert.Equal(bytes, actual);
        Assert.Equal(0, actual.Offset);
        Assert.Equal(bytes.Length, actual.Length);

        // Ensure the ctor is no-copy
        Assert.Same(ImmutableCollectionsMarshal.AsArray(bytes), actual._value);
    }

    [Theory, MemberData(nameof(EmptyImmutableArrays))]
    public void CreateFromImmutableArrayUnchecked_ProducesCorrectResultWhenEmpty(ImmutableArray<byte> bytes)
    {
        var actual = U8Unchecked.Create(bytes);

        Assert.Equal(0, actual.Offset);
        Assert.Equal(0, actual.Length);
        Assert.Null(actual._value);
        Assert.True(actual.IsEmpty);
        Assert.True(actual.Equals(bytes));
        Assert.False(actual.IsNullTerminated);
    }

    [Fact]
    public void CreateFromImmutableArrayUnchecked_DoesNotThrowOnInvalidUtf8()
    {
        ImmutableArray<byte> bytes = [0x80, 0x80, 0x80, 0x80];

        _ = U8Unchecked.Create(bytes);
    }

    static readonly int[] Numbers =
    [
        int.MinValue,
        int.MinValue + 1,
        int.MinValue + 2,
        int.MaxValue,
        int.MaxValue - 1,
        int.MaxValue - 2,
        ..Enumerable.Range(-1_000_000, 2_000_000)
    ];

    [Fact]
    public void CreateFromUtf8Formattable_ProducesCorrectResult()
    {
        var buffer = new byte[32];
        foreach (var num in Numbers)
        {
            var length = Encoding.UTF8.GetBytes(num.ToString(), buffer);
            var expected = buffer.AsSpan(0, length);
            ReadOnlySpan<U8String> actual =
            [
                num.ToU8String(),
                U8String.Create(num)
            ];

            foreach (var str in actual)
            {
                Assert.Equal(expected, str);
                Assert.Equal(0, str.Offset);
                Assert.Equal(expected.Length, str.Length);
                Assert.True(str.Equals(expected));
                Assert.True(str.IsNullTerminated);
            }
        }
    }

    [Fact]
    public void CreateFromUtf8FormattableWithFormat_ProducesCorrectResult()
    {
        var buffer = new byte[32];
        foreach (var num in Numbers)
        {
            var length = Encoding.UTF8.GetBytes(num.ToString("B"), buffer);
            var expected = buffer.AsSpan(0, length);
            ReadOnlySpan<U8String> actual =
            [
                num.ToU8String("B"),
                U8String.Create(num, "B")
            ];

            foreach (var str in actual)
            {
                Assert.Equal(expected, str);
                Assert.Equal(0, str.Offset);
                Assert.Equal(expected.Length, str.Length);
                Assert.True(str.Equals(expected));
            }
        }
    }

    [Fact]
    public void CreateFromUtf8FormattableWithCultureInfo_ProducesCorrectResult()
    {
        var dateTime = DateTime.UtcNow;
        var cultureInfo = CultureInfo.GetCultureInfo("ua-UA");

        var expected = Encoding.UTF8.GetBytes(dateTime.ToString(cultureInfo));
        var actual = new[]
        {
            dateTime.ToU8String(cultureInfo),
            U8String.Create(dateTime, cultureInfo)
        };

        foreach (var str in actual)
        {
            Assert.Equal(expected, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(expected.Length, str.Length);
            Assert.True(str.Equals(expected));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CreateFromUtf8FormattableWithFormatAndCultureInfo_ProducesCorrectResult()
    {
        var dateTime = DateTime.UtcNow;
        var cultureInfo = CultureInfo.GetCultureInfo("ua-UA");

        var expected = Encoding.UTF8.GetBytes(dateTime.ToString("F", cultureInfo));
        var actual = new[]
        {
            dateTime.ToU8String("F", cultureInfo),
            U8String.Create(dateTime, "F", cultureInfo)
        };

        foreach (var str in actual)
        {
            Assert.Equal(expected, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(expected.Length, str.Length);
            Assert.True(str.Equals(expected));
        }
    }

    [Theory, MemberData(nameof(ValidStrings))]
    public void CreateFromCharsLossy_ProducesCorrectResult(ReferenceText text)
    {
        var utf8 = text.Utf8;
        if (utf8 is []) return;

        var actual = new[]
        {
            U8String.CreateLossy(text.Utf16),
            U8String.CreateLossy(text.Utf16.AsSpan())
        };

        foreach (var str in actual)
        {
            Assert.Equal(utf8, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(utf8.Length, str.Length);
            Assert.Equal(utf8.Length + 1, str._value!.Length);
            Assert.True(str.Equals(utf8));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CreateFromCharsLossy_ProducesCorrectResultWhenEmpty()
    {
        var empty = string.Empty;
        var bytes = Array.Empty<byte>();

        var actual = new[]
        {
            U8String.CreateLossy(empty),
            U8String.CreateLossy(empty.AsSpan())
        };

        foreach (var str in actual)
        {
            Assert.Equal(bytes, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.True(str.Equals(bytes));
            Assert.False(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CreateFromCharsLossy_ReplacesTornSurrogates()
    {
        var invalid = "🔥"[..^1] + "🔥"[..^1] + "🔥"[..^1] + "🔥"[..^1];
        var expected = Encoding.UTF8.GetBytes("����");

        var actual = new[]
        {
            U8String.CreateLossy(invalid),
            U8String.CreateLossy(invalid.AsSpan())
        };

        foreach (var str in actual)
        {
            Assert.Equal(expected, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(expected.Length, str.Length);
            Assert.True(str.Equals(expected));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public void CreateFromStringLossy_ThrowsOnNullReference()
    {
        const string? value = null;

        Assert.Throws<ArgumentNullException>(() => U8String.CreateLossy(value!));
    }

    [Fact]
    public static void FromAsciiUtf16_ProducesCorrectResult()
    {
        var expected = Constants.AsciiBytes;
        var overloads = new[]
        {
            U8String.FromAscii(Constants.Ascii),
            U8String.FromAscii(Constants.Ascii.AsSpan())
        };

        foreach (var str in overloads)
        {
            Assert.Equal(expected, str);
            Assert.Equal(0, str.Offset);
            Assert.Equal(expected.Length, str.Length);
            Assert.True(str.Equals(expected));
            Assert.True(str.IsNullTerminated);
        }
    }

    [Fact]
    public static void FromAsciiUtf16_ProducesCorrectResultOnEmpty()
    {
        var overloads = new[]
        {
            U8String.FromAscii(""),
            U8String.FromAscii([])
        };

        foreach (var str in overloads)
        {
            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.Equal(0, str.Offset);
            Assert.Equal(0, str.Length);
        }
    }

    [Fact]
    public static void FromAsciiUtf16_ThrowsOnNullReference()
    {
        Assert.Throws<ArgumentNullException>(() => U8String.FromAscii(null!));
    }

    [Fact]
    public static void FromAsciiUtf16_ThrowsOnNonAscii()
    {
        Assert.Throws<FormatException>(() => U8String.FromAscii(Constants.Mixed));
        Assert.Throws<FormatException>(() => U8String.FromAscii(Constants.Mixed.AsSpan()));
    }
}
