using System.Collections.Immutable;
using System.Text;

namespace U8.Tests;

public static class Constants
{
    public static IEnumerable<Rune> AllRunes => Enumerable
        .Range(0, 0xD7FF + 1).Concat(Enumerable
        .Range(0xE000, 0x10FFFF - 0xE000 + 1))
        .Select(i => new Rune(i));

    public static readonly ImmutableArray<byte> AsciiBytes =
        [..Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i)];

    public static readonly ImmutableArray<byte> AsciiLetters = [.."ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8];

    public static readonly string Ascii = Encoding.ASCII.GetString(AsciiBytes.AsSpan());

    public const string Cyrilic =
        "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя" +
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";

    public static readonly ImmutableArray<byte> CyrilicBytes = [..Encoding.UTF8.GetBytes(Cyrilic)];

    public static IEnumerable<byte[]> CyrilicCharBytes =>
        Cyrilic.Select((_, i) => Encoding.UTF8.GetBytes(Cyrilic, i, 1));

    public const string Kana =
        "あいうえおかきくけこさしすせそたちつてとなにぬねの" +
        "はひふへほまみむめもやゆよらりるれろわをん" +
        "がぎぐげござじずぜぞだぢづでどばびぶべぼ" +
        "ぱぴぷぺぽアイウエオカキクケコサシスセソ" +
        "タチツテトナニヌネノハヒフヘホマミムメモ" +
        "ヤユヨラリルレロワヲンガギグゲゴザジズゼゾ" +
        "ダヂヅデドバビブベボパピプペポ";

    public static readonly ImmutableArray<byte> KanaBytes = [..Encoding.UTF8.GetBytes(Kana)];

    public static IEnumerable<byte[]> KanaCharBytes =>
        Kana.Select((_, i) => Encoding.UTF8.GetBytes(Kana, i, 1));

    public const string NonSurrogateEmoji =
        "😀😁😂🤣😃😄😅😆😉😊😋😎😍😘😗😙😚😐😑" +
        "😶🙄😏😣😥😮😯😪😫😴😌😛😜😝🤤😒😓😔😕" +
        "🙃🤑😲🙁😖😞😟😤😢😭😦😧😨😩😬😰😱";

    public static readonly ImmutableArray<byte> NonSurrogateEmojiBytes = [..Encoding.UTF8.GetBytes(NonSurrogateEmoji)];

    public static IEnumerable<byte[]> NonSurrogateEmojiChars =>
        NonSurrogateEmoji.EnumerateRunes().Select(Extensions.ToUtf8);

    public const string Mixed =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜß" +
        "abcäöüабв🤣😃😄😅诶比西" +
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШ" +
        "ЩЬЮЯˈkɨʂtɔfあいうえおかきくけこさしすせそ" +
        "たちつてとなにぬねのはひふへほまみむめも" +
        "やゆよらりるれろわをんがぎぐげござじずぜぞ" +
        "∈∉∊∋∌∍∎∏∐∑−∓∔∕∖∗∘∙√∛∜∝∞∟∠∡∢∣∤∥∦∧∨∩∪∫∬∭∮∯∰∱" +
        "∲∳∴∵∶∷∸∹∺∻∼∽∾∿≀≁≂≃≄≅≆≇≈≉≊≋≌≍≎≏≐≑≒≓≔≕≖≗≘≙≚" +
        "≛≜≝≞≟≠≡≢≣≤≥≦≧≨≩≪≫≬≭≮≯≰≱≲≳≴≵≶≷≸≹≺≻≼≽≾≿";

    public static readonly ImmutableArray<byte> MixedBytes = [..Encoding.UTF8.GetBytes(Mixed)];

    public static IEnumerable<byte[]> MixedCharBytes =>
        Mixed.EnumerateRunes().Select(Extensions.ToUtf8);

    public const string AsciiWhitespace = "\t\n\v\f\r ";

    public static ReadOnlySpan<byte> AsciiWhitespaceBytes => "\t\n\v\f\r "u8;

    public static readonly byte[] NonAsciiBytes =
        Enumerable.Range(0b1000_0000, 128).Select(i => (byte)i).ToArray();

    public static readonly byte[] ContinuationBytes =
        Enumerable.Range(0b1000_0000, 64).Select(i => (byte)i).ToArray();

    public static readonly byte[] BoundaryBytes =
        Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i).Concat(
        Enumerable.Range(0b1100_0000, 64).Select(i => (byte)i)).ToArray();

    public static readonly Rune[] WhitespaceRunes =
    [
        new('\t'), new('\n'), new('\v'), new('\f'), new('\r'), new(' '), // ASCII
        new(0x0085), new(0x00A0), new(0x1680), new(0x2000), new(0x2001), // Unicode
        new(0x2002), new(0x2003), new(0x2004), new(0x2005), new(0x2006),
        new(0x2007), new(0x2008), new(0x2009), new(0x200A), new(0x2028),
        new(0x2029), new(0x202F), new(0x205F), new(0x3000)
    ];

    // Covers all non-whitespace runes
    public static IEnumerable<Rune> NonWhitespaceRunes => Enumerable
        .Range(0, 0xD7FF + 1).Concat(Enumerable
        .Range(0xE000, 0x10FFFF - 0xE000 + 1))
        .Select(i => new Rune(i))
        .Except(WhitespaceRunes);

    /// <summary>
    /// Caution! Currently produces 3 294 912 permutations.
    /// Do not make methods consuming it a theory.
    /// </summary>
    public static IEnumerable<Rune[]> MixedRunePatterns()
    {
        foreach (var (first, second) in Mixed.EnumerateRunes().Permute2())
        {
            foreach (var count in Enumerable.Range(0, 16))
            {
                var firstSeq = Enumerable.Repeat(first, count);
                var secondSeq = Enumerable.Repeat(second, count);
                var pattern = firstSeq.Interleave(secondSeq);

                yield return [..pattern];
                yield return [..pattern, first];
                yield return [..pattern, second];
            }
        }
    }

    public static readonly ReferenceText[] ValidStrings =
    [
        new ReferenceText(
            Name: "Empty",
            Utf16: string.Empty,
            Utf8: [],
            Runes: []),

        new ReferenceText(
            Name: "ASCII",
            Utf16: Ascii,
            Utf8: AsciiBytes,
            Runes: [..Ascii.EnumerateRunes()]),

        new ReferenceText(
            Name: "Cyrilic",
            Utf16: Cyrilic,
            Utf8: CyrilicBytes,
            Runes: [..Cyrilic.EnumerateRunes()]),

        new ReferenceText(
            Name: "Kana",
            Utf16: Kana,
            Utf8: KanaBytes,
            Runes: [..Kana.EnumerateRunes()]),

        new ReferenceText(
            Name: "NonSurrogateEmoji",
            Utf16: NonSurrogateEmoji,
            Utf8: NonSurrogateEmojiBytes,
            Runes: [..NonSurrogateEmoji.EnumerateRunes()]),

        new ReferenceText(
            Name: "Mixed",
            Utf16: Mixed,
            Utf8: MixedBytes,
            Runes: [..Mixed.EnumerateRunes()]),
    ];
}
