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
            Utf16: Constants.Ascii,
            Utf8: Constants.AsciiBytes,
            Runes: Constants.Ascii.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Cyrilic",
            Utf16: Constants.Cyrilic,
            Utf8: Constants.CyrilicBytes,
            Runes: Constants.Cyrilic.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Kana",
            Utf16: Constants.Kana,
            Utf8: Constants.KanaBytes,
            Runes: Constants.Kana.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "NonSurrogateEmoji",
            Utf16: Constants.NonSurrogateEmoji,
            Utf8: Constants.NonSurrogateEmojiBytes,
            Runes: Constants.NonSurrogateEmoji.EnumerateRunes().ToArray()),

        new TestCase(
            Name: "Mixed",
            Utf16: Constants.Mixed,
            Utf8: Constants.MixedBytes,
            Runes: Constants.Mixed.EnumerateRunes().ToArray()),
    };
}
