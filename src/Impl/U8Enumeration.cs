using System.Runtime.InteropServices;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Unsafe opt. strided broadcast / scatter byte[] reference onto U8String[]
// in a SIMD way (is there a way to make it not explode and not be a total UB?)
internal static class U8Enumeration
{
    // TODO: Optimize? Maybe a dedicated split type or similar? Would be really nice to have const generics for this
    // Perhaps a U8SplitPair-like struct with byte[] and U8Range[] (or inline array) indices?
    // TODO 2: Consider making out Us nullable? Alternatively, consider throwing?
    internal static void Deconstruct<T, E, U>(this T source, out U first, out U second)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        // TODO: Should we throw on not match?
        (first, second) = (default, default);

        var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    internal static void Deconstruct<T, E, U>(this T source, out U first, out U second, out U third)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        (first, second, third) = (default, default, default);

        var enumerator = source.GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    third = enumerator.Current;
                }
            }
        }
    }

    internal static void CopyTo<T, E, U>(this ref T source, Span<U> destination)
        where T : struct, IEnumerable<U, E>, ICollection<U>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var count = source.Count;
        if (count is not 0)
        {
            source.FillUnchecked<T, E, U>(destination[..count]);
        }
    }

    internal static U[] ToArray<T, E, U>(this ref T source)
        where T : struct, IEnumerable<U, E>, ICollection<U>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var count = source.Count;
        if (count is not 0)
        {
            var result = new U[count];
            source.FillUnchecked<T, E, U>(result);

            return result;
        }

        return Array.Empty<U>();
    }

    internal static List<U> ToList<T, E, U>(this ref T source)
        where T : struct, IEnumerable<U, E>, ICollection<U>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var count = source.Count;
        var result = new List<U>(count);

        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        source.FillUnchecked<T, E, U>(span);

        return result;
    }

    private static void FillUnchecked<T, E, U>(this ref T source, Span<U> destination)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var i = 0;
        ref var ptr = ref destination.AsRef();
        foreach (var item in source)
        {
            ptr.Add(i++) = item;
        }
    }
}
