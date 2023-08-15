using System.Text;

namespace U8Primitives.Tests;

public static class TestConstants
{
    public static readonly byte[] AsciiBytes =
        Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i).ToArray();

    public static ReadOnlySpan<byte> AsciiWhitespaceBytes => "\t\n\v\f\r "u8;

    public static readonly byte[] NonAsciiBytes =
        Enumerable.Range(0b1000_0000, 128).Select(i => (byte)i).ToArray();

    public static readonly byte[] ContinuationBytes =
        Enumerable.Range(0b1000_0000, 64).Select(i => (byte)i).ToArray();

    public static readonly byte[] NonContinuationBytes =
        Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i).Concat(
        Enumerable.Range(0b1100_0000, 64).Select(i => (byte)i)).ToArray();

    public static readonly string Ascii = Encoding.ASCII.GetString(AsciiBytes);

    public const string Cyrilic =
        "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя" +
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";

    public static readonly byte[] CyrilicBytes = Encoding.UTF8.GetBytes(Cyrilic);

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

    public static readonly byte[] KanaBytes = Encoding.UTF8.GetBytes(Kana);

    public static IEnumerable<byte[]> KanaCharBytes =>
        Kana.Select((_, i) => Encoding.UTF8.GetBytes(Kana, i, 1));

    public const string NonSurrogateEmoji =
        "😀😁😂🤣😃😄😅😆😉😊😋😎😍😘😗😙😚😐😑" +
        "😶🙄😏😣😥😮😯😪😫😴😌😛😜😝🤤😒😓😔😕" +
        "🙃🤑😲🙁😖😞😟😤😢😭😦😧😨😩😬😰😱";

    public static readonly byte[] NonSurrogateEmojiBytes = Encoding.UTF8.GetBytes(NonSurrogateEmoji);

    public static IEnumerable<byte[]> NonSurrogateEmojiChars =>
        NonSurrogateEmoji.EnumerateRunes().Select(r =>
        {
            var buf = (stackalloc byte[32]);
            var len = r.EncodeToUtf8(buf);
            return buf[..len].ToArray();
        });

    public const string Mixed =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcあいうабв🤣😃😄😅诶比西" +
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШ" +
        "ЩЬЮЯあいうえおかきくけこさしすせそ" +
        "たちつてとなにぬねのはひふへほまみむめも" +
        "やゆよらりるれろわをんがぎぐげござじずぜぞ" +
        "∈∉∊∋∌∍∎∏∐∑−∓∔∕∖∗∘∙√∛∜∝∞∟∠∡∢∣∤∥∦∧∨∩∪∫∬∭∮∯∰∱" +
        "∲∳∴∵∶∷∸∹∺∻∼∽∾∿≀≁≂≃≄≅≆≇≈≉≊≋≌≍≎≏≐≑≒≓≔≕≖≗≘≙≚" +
        "≛≜≝≞≟≠≡≢≣≤≥≦≧≨≩≪≫≬≭≮≯≰≱≲≳≴≵≶≷≸≹≺≻≼≽≾≿";

    public static readonly byte[] MixedBytes = Encoding.UTF8.GetBytes(Mixed);

    public static IEnumerable<byte[]> MixedCharBytes =>
        Mixed.EnumerateRunes().Select(r =>
        {
            var buf = (stackalloc byte[32]);
            var len = r.EncodeToUtf8(buf);
            return buf[..len].ToArray();
        });

}
