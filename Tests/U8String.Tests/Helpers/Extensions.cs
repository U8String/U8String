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

    internal static T ParsableParse<T>(string s) where T : IParsable<T>
    {
        return T.Parse(s, null);
    }

    internal static bool ParsableTryParse<T>(string? s, out T? result) where T : IParsable<T>
    {
        return T.TryParse(s, null, out result);
    }

    internal static T SpanParsableParse<T>(ReadOnlySpan<char> s) where T : ISpanParsable<T>
    {
        return T.Parse(s, null);
    }

    internal static bool SpanParsableTryParse<T>(ReadOnlySpan<char> s, out T? result) where T : ISpanParsable<T>
    {
        return T.TryParse(s, null, out result);
    }

    internal static T Utf8SpanParsableParse<T>(ReadOnlySpan<byte> utf8Text) where T : IUtf8SpanParsable<T>
    {
        return T.Parse(utf8Text, null);
    }

    internal static bool Utf8SpanParsableTryParse<T>(ReadOnlySpan<byte> utf8Text, out T? result) where T : IUtf8SpanParsable<T>
    {
        return T.TryParse(utf8Text, null, out result);
    }

    internal static ReadOnlySpan<T> Span<T>(ReadOnlySpan<T> span) => span;

    internal static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
    {
        return source.SelectMany(x => x);
    }

    internal static IEnumerable<U> FlatMap<T, U>(this IEnumerable<(T, T)> source, Func<T, T, IEnumerable<U>> selector)
    {
        return source.SelectMany(x => selector(x.Item1, x.Item2));
    }

    internal static IEnumerable<T> Interleave<T>(this IEnumerable<T> first, IEnumerable<T> second)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();

        var cont1 = true;
        var cont2 = true;
        
        while (cont1 || cont2)
        {
            if (cont1) cont1 = e1.MoveNext();
            if (cont1) yield return e1.Current;
            if (cont2) cont2 = e2.MoveNext();
            if (cont2) yield return e2.Current;
        }
    }

    internal static IEnumerable<T> Interleave<T>(this IEnumerable<T> first, IEnumerable<T> second, IEnumerable<T> third)
    {
        using var e1 = first.GetEnumerator();
        using var e2 = second.GetEnumerator();
        using var e3 = third.GetEnumerator();

        var cont1 = true;
        var cont2 = true;
        var cont3 = true;
        
        while (cont1 || cont2 || cont3)
        {
            if (cont1) cont1 = e1.MoveNext();
            if (cont1) yield return e1.Current;
            if (cont2) cont2 = e2.MoveNext();
            if (cont2) yield return e2.Current;
            if (cont3) cont3 = e3.MoveNext();
            if (cont3) yield return e3.Current;
        }
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
