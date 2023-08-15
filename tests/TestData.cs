using System.Text;

namespace U8Primitives.Tests;

public static class TestData
{
    public static readonly TestCase[] ValidStrings = new[]
    {
        new TestCase(
            Name: "Empty",
            Utf16: string.Empty,
            Utf8: Array.Empty<byte>(),
            Runes: Array.Empty<Rune>()),

        new TestCase(
            Name: "ASCII",
            Utf16: TestConstants.Ascii,
            Utf8: TestConstants.AsciiBytes,
            Runes: TestConstants.Ascii.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Cyrilic",
            Utf16: TestConstants.Cyrilic,
            Utf8: TestConstants.CyrilicBytes,
            Runes: TestConstants.Cyrilic.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Kana",
            Utf16: TestConstants.Kana,
            Utf8: TestConstants.KanaBytes,
            Runes: TestConstants.Kana.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "NonSurrogateEmoji",
            Utf16: TestConstants.NonSurrogateEmoji,
            Utf8: TestConstants.NonSurrogateEmojiBytes,
            Runes: TestConstants.NonSurrogateEmoji.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Mixed",
            Utf16: TestConstants.Mixed,
            Utf8: TestConstants.MixedBytes,
            Runes: TestConstants.Mixed.EnumerateRunes().ToArray()),
    };
}
