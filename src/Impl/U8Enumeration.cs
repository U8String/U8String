using System.Runtime.InteropServices;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Unsafe opt. strided broadcast / scatter byte[] reference onto U8String[]
// in a SIMD way (is there a way to make it not explode and not be a total UB?)
internal static class U8Enumeration
{
    internal static void CopyTo<T, E>(this ref T source, Span<U8String> destination)
        where T : struct, ICollection<U8String>, IU8Enumerable<E>
        where E : struct, IU8Enumerator
    {
        var count = source.Count;
        if (count is not 0)
        {
            source.FillUnchecked<T, E, U8String>(destination[..count]);
        }
    }

    internal static U8String[] ToArray<T, E>(this ref T source)
        where T : struct, ICollection<U8String>, IU8Enumerable<E>
        where E : struct, IU8Enumerator
    {
        var count = source.Count;
        if (count is not 0)
        {
            var result = new U8String[count];
            source.FillUnchecked<T, E, U8String>(result);

            return result;
        }

        return Array.Empty<U8String>();
    }

    internal static List<U8String> ToList<T, E>(this ref T source)
        where T : struct, ICollection<U8String>, IU8Enumerable<E>
        where E : struct, IU8Enumerator
    {
        var count = source.Count;
        var result = new List<U8String>(count);

        CollectionsMarshal.SetCount(result, count);
        var span = CollectionsMarshal.AsSpan(result);
        source.FillUnchecked<T, E, U8String>(span);

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
