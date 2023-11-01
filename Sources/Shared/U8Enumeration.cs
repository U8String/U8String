using System.Runtime.InteropServices;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Unsafe opt. strided broadcast / scatter byte[] reference onto U8String[]
// in a SIMD way (is there a way to make it not explode and not be a total UB?)
internal static class U8Enumeration
{
    internal static void CopyTo<T, E, U>(this T source, Span<U> destination)
        where T : struct, IEnumerable<U, E>, ICollection<U>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var count = source.Count;
        if (count > 0)
        {
            source.FillUnchecked<T, E, U>(destination[..count]);
        }
    }

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

    internal static U ElementAt<T, E, U>(this T source, int index)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        if (index < 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        foreach (var item in source)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return ThrowHelpers.ArgumentOutOfRange<U>();
    }

    internal static U ElementAtOrDefault<T, E, U>(this T source, int index)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        if (index < 0)
        {
            return default;
        }

        foreach (var item in source)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return default;
    }

    // internal static U First<T, E, U>(this T source)
    //     where T : struct, IEnumerable<U, E>
    //     where E : struct, IEnumerator<U>
    //     where U : struct
    // {
    //     var enumerator = source.GetEnumerator();
    //     if (enumerator.MoveNext())
    //     {
    //         return enumerator.Current;
    //     }

    //     return ThrowHelpers.SequenceIsEmpty<U>();
    // }

    // internal static U FirstOrDefault<T, E, U>(this T source)
    //     where T : struct, IEnumerable<U, E>
    //     where E : struct, IEnumerator<U>
    //     where U : struct
    // {
    //     var enumerator = source.GetEnumerator();
    //     if (enumerator.MoveNext())
    //     {
    //         return enumerator.Current;
    //     }

    //     return default;
    // }

    // internal static U Last<T, E, U>(this T source)
    //     where T : struct, IEnumerable<U, E>
    //     where E : struct, IEnumerator<U>
    //     where U : struct
    // {
    //     // TODO: Use LastIndexOf on splits? Replace this with analyzer to use SplitFirst/Last?
    //     var enumerator = source.GetEnumerator();
    //     if (enumerator.MoveNext())
    //     {
    //         var result = enumerator.Current;
    //         while (enumerator.MoveNext())
    //         {
    //             result = enumerator.Current;
    //         }

    //         return result;
    //     }

    //     return ThrowHelpers.SequenceIsEmpty<U>();
    // }

    internal static U[] ToArray<T, E, U>(this T source)
        where T : struct, IEnumerable<U, E>, ICollection<U>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var count = source.Count;
        if (count > 0)
        {
            var result = new U[count];
            source.FillUnchecked<T, E, U>(result);

            return result;
        }

        return [];
    }

    internal static List<U> ToList<T, E, U>(this T source)
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

    // TODO: Refactor to ArrayBuilder (generic variant)
    internal static U[] ToArrayUnsized<T, E, U>(this T source, int maxLength)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var result = new U[maxLength];
        var count = source.FillUnchecked<T, E, U>(result);
        if (count != maxLength)
        {
            Array.Resize(ref result, count);
        }

        return result;
    }

    internal static List<U> ToListUnsized<T, E, U>(this T source, int maxLength)
        where T : struct, IEnumerable<U, E>
        where E : struct, IEnumerator<U>
        where U : struct
    {
        var result = new List<U>(maxLength);

        CollectionsMarshal.SetCount(result, maxLength);
        var span = CollectionsMarshal.AsSpan(result);
        var count = source.FillUnchecked<T, E, U>(span);
        CollectionsMarshal.SetCount(result, count);

        return result;
    }

    static int FillUnchecked<T, E, U>(this T source, Span<U> destination)
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

        return i;
    }
}
