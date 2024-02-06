using System.Buffers;
using System.Text;

namespace U8.Tests;

internal static class Extensions
{
    internal static byte[] ToUtf8(this Rune rune)
    {
        var buf = (stackalloc byte[4]);
        var len = rune.EncodeToUtf8(buf);
        return buf[..len].ToArray();
    }

    internal static Rune ToRune(this byte[] utf8)
    {
        return Rune.DecodeFromUtf8(utf8, out var rune, out var bytesConsumed)
            is OperationStatus.Done && bytesConsumed == utf8.Length
                ? rune : throw new Exception("Invalid UTF-8");
    }

    internal static int Utf8Length(this char c)
    {
        return (ushort)c switch
        {
            <= 0x7F => 1,
            <= 0x7FF => 2,
            _ => 3
        };
    }

    internal static T[] Array<T>(T[] array) => array;

    internal static ReadOnlySpan<T> Span<T>(ReadOnlySpan<T> span) => span;

    internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
    {
        return source.SelectMany(x => x);
    }

    internal static IEnumerable<U> FlatMap<T, U>(this IEnumerable<(T, T)> source, Func<T, T, IEnumerable<U>> selector)
    {
        return source.SelectMany(x => selector(x.Item1, x.Item2));
    }

    internal static IEnumerable<(T X, T Y)> Permute2<T>(this IEnumerable<T> source)
    {
        return source.SelectMany(x => source, (x, y) => (x, y));
    }

    internal static IEnumerable<T[]> WithArgsLimit<T>(this IEnumerable<T[]> source, int arity)
    {
        return source.Select(args => args.Take(arity).ToArray());
    }
}
