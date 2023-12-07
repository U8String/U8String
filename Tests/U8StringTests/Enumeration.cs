using System.Buffers;
using System.Text;

namespace U8Primitives.Tests.U8StringTests;

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

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_CollectsToCorrectArray(ReferenceText testCase)
    {
        var chars = testCase.Utf16.AsSpan();
        var u8chars = new U8String(testCase.Utf8).Chars;

        Assert.True(chars.SequenceEqual(u8chars.ToArray()));
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
            Assert.Contains(r, u8runes);
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

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_CollectsToCorrectArray(ReferenceText testCase)
    {
        var runes = testCase.Runes.AsSpan();
        var u8runes = new U8String(testCase.Utf8).Runes;

        Assert.True(runes.SequenceEqual(u8runes.ToArray()));
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

            notMatch = new U8RuneIndex(
                r.Value is 0 ? new Rune(0x1) : default,
                offset,
                length);
            Assert.False(indices.Contains(notMatch));
            Assert.DoesNotContain(notMatch, indices);
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
}
