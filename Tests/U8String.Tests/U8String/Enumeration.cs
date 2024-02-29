using System.Buffers;
using System.Text;

using U8.Primitives;

namespace U8.Tests.U8StringTests;

#pragma warning disable CA1829, RCS1077 // Optimize LINQ method call.
#pragma warning disable xUnit2017 // xUnit analyzer suggests changes that are wrong
// TODO:
// - ElementAt returns correct value, throws
// - CopyTo writes correct sequence, throws
// - ToList returns correct sequence
public class Enumeration
{
    public static IEnumerable<object[]> ValidStrings => Constants.ValidStrings.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void ByteEnumerator_ReturnsCorrectBytes(ReferenceText testCase)
    {
        var bytes = testCase.Utf8;
        var u8str = new U8String(testCase.Utf8);

        // Non-boxed enumeration
        var enumerator = u8str.GetEnumerator();
        foreach (var b in bytes)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(b, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(bytes, u8str);
    }

    [Fact]
    public void ByteEnumerator_ReturnsCorrectBytesOnShiftedOffset()
    {
        var bytes = Constants.AsciiBytes;
        var u8str = (U8String)bytes;

        bytes = bytes[8..];
        u8str = u8str[8..];

        // Non-boxed enumeration
        var enumerator = u8str.GetEnumerator();
        foreach (var b in bytes)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(b, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(bytes, u8str);
    }

    [Fact]
    public void ByteEnumerator_ReturnsDefaultCurrentWhenDefault()
    {
        var enumerator = default(U8String.Enumerator);

        Assert.Equal(default, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_CountHasCorrectValue(ReferenceText testCase)
    {
        var chars = testCase.Utf16;
        var u8str = new U8String(testCase.Utf8);
        var u8chars = u8str.Chars;

        // First evaluation
        Assert.Equal(chars.Length, u8chars.Count);
        // Boxed evaluation
        Assert.Equal(chars.Length, u8str.Chars.Count());
    }

    [Fact]
    public void U8Chars_CountHasCorrectValueOnShiftedOffset()
    {
        var chars = Constants.Ascii;
        var u8str = (U8String)Constants.AsciiBytes;

        chars = chars[8..];
        u8str = u8str[8..];

        // First evaluation
        Assert.Equal(chars.Length, u8str.Chars.Count);
        // Boxed evaluation
        Assert.Equal(chars.Length, u8str.Chars.Count());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_ContainsReturnsCorrectValue(ReferenceText testCase)
    {
        var chars = testCase.Utf16;
        var u8str = new U8String(testCase.Utf8);
        var u8chars = u8str.Chars;

        foreach (var c in chars)
        {
            if (!char.IsSurrogate(c))
            {
                // Regular evaluation
                Assert.True(u8chars.Contains(c));
                // Boxed evaluation
                Assert.True(((IEnumerable<char>)u8chars).Contains(c));
            }
            else
            {
                Assert.Throws<ArgumentException>(() => u8chars.Contains(c));
                Assert.Throws<ArgumentException>(() => ((IEnumerable<char>)u8chars).Contains(c));
            }
        }
    }

    [Fact]
    public void U8Chars_ContainsReturnsCorrectValueOnShiftedOffset()
    {
        var chars = Constants.Ascii;
        var u8str = (U8String)Constants.AsciiBytes;

        chars = chars[8..];
        u8str = u8str[8..];

        foreach (var c in chars)
        {
            // Regular evaluation
            Assert.True(u8str.Chars.Contains(c));
            // Boxed evaluation
            Assert.True(((IEnumerable<char>)u8str.Chars).Contains(c));
        }
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_EnumeratesToCorrectValues(ReferenceText testCase)
    {
        var chars = testCase.Utf16;
        var u8str = new U8String(testCase.Utf8);
        var u8chars = u8str.Chars;

        // Non-boxed enumeration
        var enumerator = u8chars.GetEnumerator();
        foreach (var c in chars)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(c, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(chars.ToArray(), u8chars);
    }

    [Fact]
    public void U8Chars_EnumeratesToCorrectValuesOnShiftedOffset()
    {
        var chars = Constants.Ascii;
        var u8str = (U8String)Constants.AsciiBytes;

        chars = chars[8..];
        u8str = u8str[8..];

        // Non-boxed enumeration
        var enumerator = u8str.Chars.GetEnumerator();
        foreach (var c in chars)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(c, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(chars.ToArray(), u8str.Chars);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_CollectsToCorrectArray(ReferenceText testCase)
    {
        var chars = testCase.Utf16.AsSpan();
        var u8chars = new U8String(testCase.Utf8).Chars;

        Assert.True(chars.SequenceEqual(u8chars.ToArray()));
    }

    [Fact]
    public void U8Chars_CollectsToCorrectArrayOnShiftedOffset()
    {
        var chars = Constants.Ascii.AsSpan();
        var u8str = (U8String)Constants.AsciiBytes;

        chars = chars[8..];
        u8str = u8str[8..];

        Assert.True(chars.SequenceEqual(u8str.Chars.ToArray()));
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_CountHasCorrectValue(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var u8runes = u8str.Runes;

        // First evaluation
        Assert.Equal(runes.Length, u8runes.Count);
        // Boxed evaluation
        Assert.Equal(runes.Length, u8str.Runes.Count());
    }

    [Fact]
    public void U8Runes_CountHasCorrectValueOnShiftedOffset()
    {
        var runes = Constants.Ascii.EnumerateRunes().ToArray();
        var u8str = (U8String)Constants.AsciiBytes;

        runes = runes[8..];
        u8str = u8str[8..];

        // First evaluation
        Assert.Equal(runes.Length, u8str.Runes.Count);
        // Boxed evaluation
        Assert.Equal(runes.Length, u8str.Runes.Count());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_ContainsReturnsCorrectValue(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var u8runes = u8str.Runes;

        foreach (var r in runes)
        {
            // Regular evaluation
            Assert.True(u8runes.Contains(r));
            // Boxed evaluation
            Assert.True(((ICollection<Rune>)u8str.Runes).Contains(r));
        }
    }

    [Fact]
    public void U8Runes_ContainsReturnsCorrectValueOnShiftedOffset()
    {
        var runes = Constants.Ascii.EnumerateRunes().ToArray();
        var u8str = (U8String)Constants.AsciiBytes;

        runes = runes[8..];
        u8str = u8str[8..];

        foreach (var r in runes)
        {
            // Regular evaluation
            Assert.True(u8str.Runes.Contains(r));
            // Boxed evaluation
            Assert.True(((ICollection<Rune>)u8str.Runes).Contains(r));
        }
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_EnumeratesToCorrectValues(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var u8runes = u8str.Runes;

        // Non-boxed enumeration
        var enumerator = u8runes.GetEnumerator();
        foreach (var r in runes)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(r, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(runes, u8runes);
    }

    [Fact]
    public void U8Runes_EnumeratesToCorrectValuesOnShiftedOffset()
    {
        var runes = Constants.Ascii.EnumerateRunes().ToArray();
        var u8str = (U8String)Constants.AsciiBytes;

        runes = runes[8..];
        u8str = u8str[8..];

        // Non-boxed enumeration
        var enumerator = u8str.Runes.GetEnumerator();
        foreach (var r in runes)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(r, enumerator.Current);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(runes, u8str.Runes);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_CollectsToCorrectArray(ReferenceText testCase)
    {
        var runes = testCase.Runes.AsSpan();
        var u8runes = new U8String(testCase.Utf8).Runes;

        Assert.True(runes.SequenceEqual(u8runes.ToArray()));
    }

    [Fact]
    public void U8Runes_CollectsToCorrectArrayOnShiftedOffset()
    {
        var runes = Constants.Ascii.EnumerateRunes().ToArray();
        var u8str = (U8String)Constants.AsciiBytes;

        runes = runes[8..];
        u8str = u8str[8..];

        Assert.True(runes.SequenceEqual(u8str.Runes.ToArray()));
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8RuneIndices_CountHasCorrectValue(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var indices = u8str.RuneIndices;

        // First evaluation
        Assert.Equal(runes.Length, indices.Count);
        // Boxed evaluation
        Assert.Equal(runes.Length, u8str.RuneIndices.Count());
    }

    [Fact]
    public void U8RuneIndices_CountHasCorrectValueOnShiftedOffset()
    {
        var runes = Constants.Ascii.EnumerateRunes().ToArray();
        var u8str = (U8String)Constants.AsciiBytes;

        runes = runes[8..];
        u8str = u8str[8..];

        // First evaluation
        Assert.Equal(runes.Length, u8str.RuneIndices.Count);
        // Boxed evaluation
        Assert.Equal(runes.Length, u8str.RuneIndices.Count());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8RuneIndices_ContainsReturnsCorrectValue(ReferenceText testCase)
    {
        // This test assumes that all test cases contain non-repeating runes
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var indices = u8str.RuneIndices;

        foreach (var r in runes)
        {
            // This uses verified CoreLib implementation
            var runeBytes = r.ToUtf8();
            var offset = testCase.Utf8.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;

            var runeIndex = new U8RuneIndex(r, offset, length);

            Assert.True(indices.Contains(runeIndex));
            // Boxed evaluation
            Assert.Contains(runeIndex, indices);

            var notMatch = new U8RuneIndex(r, offset + 1, length);
            Assert.False(indices.Contains(notMatch));
            Assert.DoesNotContain(notMatch, indices);

            // When rune is default(T), make sure we are using something different.
            notMatch = new U8RuneIndex(
                r.Value is 0 ? new Rune(0x1) : default,
                offset,
                length);
            Assert.False(indices.Contains(notMatch));
            Assert.DoesNotContain(notMatch, indices);
        }
    }

    [Fact]
    public void U8RuneIndices_ContainsReturnsCorrectValueOnShiftedOffset()
    {
        // This test assumes that all test cases contain non-repeating runes
        var chars = Constants.Ascii;
        var bytes = Constants.AsciiBytes;
        var u8str = (U8String)bytes;

        chars = chars[8..];
        bytes = bytes[8..];
        u8str = u8str[8..];

        foreach (var r in chars.EnumerateRunes())
        {
            // This uses verified CoreLib implementation
            var runeBytes = r.ToUtf8();
            var offset = bytes.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;

            var runeIndex = new U8RuneIndex(r, offset, length);

            Assert.True(u8str.RuneIndices.Contains(runeIndex));
            // Boxed evaluation
            Assert.True(((ICollection<U8RuneIndex>)u8str.RuneIndices).Contains(runeIndex));

            var notMatch = new U8RuneIndex(r, offset + 1, length);
            Assert.False(u8str.RuneIndices.Contains(notMatch));
            Assert.DoesNotContain(notMatch, u8str.RuneIndices);

            notMatch = new U8RuneIndex(
                r.Value is 0 ? new Rune(0x1) : default,
                offset,
                length);
            Assert.False(u8str.RuneIndices.Contains(notMatch));
            Assert.DoesNotContain(notMatch, u8str.RuneIndices);
        }
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8RuneIndices_EnumeratesToCorrectValues(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);

        var actualIndices = u8str.RuneIndices;
        var expectedIndices = runes.Select(r =>
        {
            var runeBytes = r.ToUtf8();
            var offset = testCase.Utf8.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;
            return new U8RuneIndex(r, offset, length);
        });

        // Non-boxed enumeration
        var enumerator = actualIndices.GetEnumerator();
        foreach (var expected in expectedIndices)
        {
            Assert.True(enumerator.MoveNext());

            var actual = enumerator.Current;
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.Offset, actual.Offset);
            Assert.Equal(expected.Length, actual.Length);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(expectedIndices, actualIndices);
    }

    [Fact]
    public void U8RuneIndices_EnumeratesToCorrectValuesOnShiftedOffset()
    {
        var chars = Constants.Ascii;
        var bytes = Constants.AsciiBytes;
        var u8str = (U8String)bytes;

        chars = chars[8..];
        bytes = bytes[8..];
        u8str = u8str[8..];

        var actualIndices = u8str.RuneIndices;
        var expectedIndices = chars.EnumerateRunes().Select(r =>
        {
            var runeBytes = r.ToUtf8();
            var offset = bytes.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;
            return new U8RuneIndex(r, offset, length);
        });

        // Non-boxed enumeration
        var enumerator = actualIndices.GetEnumerator();
        foreach (var expected in expectedIndices)
        {
            Assert.True(enumerator.MoveNext());

            var actual = enumerator.Current;
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.Offset, actual.Offset);
            Assert.Equal(expected.Length, actual.Length);
        }

        // Guard against misbehaving consuming implementations
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        // Boxed enumeration
        Assert.Equal(expectedIndices, actualIndices);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8RuneIndices_CollectsToCorrectArray(ReferenceText testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);

        var actual = u8str.RuneIndices.ToArray();
        var expected = runes.Select(r =>
        {
            var runeBytes = r.ToUtf8();
            var offset = testCase.Utf8.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;
            return new U8RuneIndex(r, offset, length);
        }).ToArray();

        Assert.True(actual.AsSpan().SequenceEqual(expected));
    }

    [Fact]
    public void U8RuneIndices_CollectsToCorrectArrayOnShiftedOffset()
    {
        var chars = Constants.Ascii;
        var bytes = Constants.AsciiBytes;
        var u8str = (U8String)bytes;

        chars = chars[8..];
        bytes = bytes[8..];
        u8str = u8str[8..];

        var actual = u8str.RuneIndices.ToArray();
        var expected = chars.EnumerateRunes().Select(r =>
        {
            var runeBytes = r.ToUtf8();
            var offset = bytes.AsSpan().IndexOf(runeBytes);
            var length = runeBytes.Length;
            return new U8RuneIndex(r, offset, length);
        }).ToArray();

        Assert.True(actual.AsSpan().SequenceEqual(expected));
    }
}
