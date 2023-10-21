using System.Buffers;

namespace U8Primitives.Tests;

#pragma warning disable CA1829, RCS1077 // Optimize LINQ method call.
#pragma warning disable xUnit2017 // xUnit analyzer suggests changes that are wrong
// TODO:
// - ElementAt returns correct value, throws
// - CopyTo writes correct sequence, throws
// - ToList returns correct sequence
public class U8EnumeratorTests
{
    public static readonly IEnumerable<object[]> ValidStrings = new[]
    {
        new TestCase(
            Name: "Empty",
            Utf16: string.Empty,
            Utf8: [],
            Runes: []),

        new TestCase(
            Name: "ASCII",
            Utf16: Constants.Ascii,
            Utf8: Constants.AsciiBytes,
            Runes: [..Constants.Ascii.EnumerateRunes()]),

        new TestCase(
            Name: "Cyrilic",
            Utf16: Constants.Cyrilic,
            Utf8: Constants.CyrilicBytes,
            Runes: [..Constants.Cyrilic.EnumerateRunes()]),

        new TestCase(
            Name: "Kana",
            Utf16: Constants.Kana,
            Utf8: Constants.KanaBytes,
            Runes: [..Constants.Kana.EnumerateRunes()]),

        new TestCase(
            Name: "NonSurrogateEmoji",
            Utf16: Constants.NonSurrogateEmoji,
            Utf8: Constants.NonSurrogateEmojiBytes,
            Runes: [..Constants.NonSurrogateEmoji.EnumerateRunes()]),

        new TestCase(
            Name: "Mixed",
            Utf16: Constants.Mixed,
            Utf8: Constants.MixedBytes,
            Runes: [..Constants.Mixed.EnumerateRunes()]),
    }.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void ByteEnumerator_ReturnsCorrectBytes(TestCase testCase)
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
    public void U8Chars_CountHasCorrectValue(TestCase testCase)
    {
        var chars = testCase.Utf16;
        var u8str = new U8String(testCase.Utf8);
        var u8chars = u8str.Chars;

        // First evaluation
        Assert.Equal(chars.Length, u8chars.Count);
        // Cached evaluation
        Assert.Equal(chars.Length, u8chars.Count);
        // Boxed evaluation
        Assert.Equal(chars.Length, u8str.Chars.Count());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_ContainsReturnsCorrectValue(TestCase testCase)
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
    public void U8Chars_EnumeratesToCorrectValues(TestCase testCase)
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
    public void U8Chars_CollectsToCorrectArray(TestCase testCase)
    {
        var chars = testCase.Utf16.AsSpan();
        var u8chars = new U8String(testCase.Utf8).Chars;

        Assert.True(chars.SequenceEqual(u8chars.ToArray()));
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_CountHasCorrectValue(TestCase testCase)
    {
        var runes = testCase.Runes;
        var u8str = new U8String(testCase.Utf8);
        var u8runes = u8str.Runes;

        // First evaluation
        Assert.Equal(runes.Length, u8runes.Count);
        // Cached evaluation
        Assert.Equal(runes.Length, u8runes.Count);
        // Boxed evaluation
        Assert.Equal(runes.Length, u8str.Runes.Count());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void U8Runes_ContainsReturnsCorrectValue(TestCase testCase)
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
    public void U8Runes_EnumeratesToCorrectValues(TestCase testCase)
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
    public void U8Runes_CollectsToCorrectArray(TestCase testCase)
    {
        var runes = testCase.Runes.AsSpan();
        var u8runes = new U8String(testCase.Utf8).Runes;

        Assert.True(runes.SequenceEqual(u8runes.ToArray()));
    }
}
