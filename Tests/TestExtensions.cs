using System.Buffers;
using System.Text;

namespace U8Primitives.Tests;

internal static class TestExtensions
{
    internal static byte[] ToUtf8(this Rune rune)
    {
        var buf = (stackalloc byte[4]);
        var len = rune.EncodeToUtf8(buf);
        return buf[..len].ToArray();
    }

    internal static Rune ToRune(this byte[] utf8)
    {
        return Rune.DecodeFromUtf8(utf8, out var rune, out var bytesConsumed) is OperationStatus.Done
            && bytesConsumed == utf8.Length
                ? rune : throw new Exception("Invalid UTF-8");
    }
}
