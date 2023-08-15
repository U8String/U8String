using System.Buffers;

namespace U8Primitives.Tests;

#pragma warning disable CA1829, RCS1077 // Optimize LINQ method call.
#pragma warning disable xUnit1004 // Test methods should not be skipped
#pragma warning disable xUnit2017 // xUnit analyzer suggests changes that are wrong
public class U8EnumerationTests
{
    // TODO: Refactor?
    public static IEnumerable<object[]> ValidStrings =>
        TestData.ValidStrings.Select(c => new object[] { c });

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

    [Theory(Skip = "This is absolutely broken because you can't transcode a single char from a surrogate pair to UTF-8." +
                   "Skipping for now until I decide on the best way to handle this without making suboptimal tradeoffs.")]
    [MemberData(nameof(ValidStrings))]
    public void U8Chars_ContainsReturnsCorrectValue(TestCase testCase)
    {
        var chars = testCase.Utf16;
        var u8str = new U8String(testCase.Utf8);
        var u8chars = u8str.Chars;

        foreach (var c in chars)
        {
            // Regular evaluation
            Assert.True(u8chars.Contains(c));
            // Boxed evaluation
            Assert.Contains(c, u8chars);
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
        Assert.Equal(chars, u8chars);
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
