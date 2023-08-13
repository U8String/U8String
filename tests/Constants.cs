using System.Text;

namespace U8Primitives.Tests;

public static class Constants
{
    public static IEnumerable<byte> AsciiBytes =>
        Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i);

    public static ReadOnlySpan<byte> AsciiWhitespaceBytes => "\t\n\v\f\r "u8;

    public static IEnumerable<byte> NonAsciiBytes =>
        Enumerable.Range(0b1000_0000, 128).Select(i => (byte)i);

    public static IEnumerable<byte> ContinuationBytes =>
        Enumerable.Range(0b1000_0000, 64).Select(i => (byte)i);

    public static IEnumerable<byte> NonContinuationBytes =>
        Enumerable.Range(0b0000_0000, 128).Select(i => (byte)i).Concat(
        Enumerable.Range(0b1100_0000, 64).Select(i => (byte)i));

    public const string Cyrilic =
        "абвгґдеєжзиіїйклмнопрстуфхцчшщьюя" +
        "АБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";

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

    public static IEnumerable<byte[]> KanaCharBytes =>
        Kana.Select((_, i) => Encoding.UTF8.GetBytes(Kana, i, 1));

    public const string NonSurrogateEmoji = 
        "😀😁😂🤣😃😄😅😆😉😊😋😎😍😘😗😙😚😐😑" +
        "😶🙄😏😣😥😮😯😪😫😴😌😛😜😝🤤😒😓😔😕" +
        "🙃🤑😲🙁😖😞😟😤😢😭😦😧😨😩😬😰😱";

    public static IEnumerable<byte[]> NonSurrogateEmojiChars =>
        NonSurrogateEmoji.EnumerateRunes().Select(r =>
        {
            var buf = (stackalloc byte[32]);
            var len = r.EncodeToUtf8(buf);
            return buf[..len].ToArray();
        });
}
