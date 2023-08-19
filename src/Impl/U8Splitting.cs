using System.Runtime.InteropServices;
using U8Primitives.Abstractions;

namespace U8Primitives;

internal static class U8Splitting
{
    // We cannot express this through a struct because (uint, byte) completely
    // messes up layouts of enclosing structs, which means separator must always
    // be passed in a flattened form.
    // TODO: Consider sacrificing the ability to make compact/efficient split structs
    // for runes and create a U8TruncatedChar (or better name?) expressed as (byte x3 Char, byte Length)
    // (or not, because it will cause separate single byte loads due to alignment, unless we bitwise unpack
    // but then bitwise unpack can and likely will break constant prop ruining codegen in a different way, can't win)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> CreateSeparator(in uint value, byte length)
    {
        ref var ptr = ref Unsafe.As<uint, byte>(ref Unsafe.AsRef(in value));
        return MemoryMarshal.CreateReadOnlySpan(ref ptr, length);
    }

    // TODO: Optimize? Maybe a dedicated split type or similar? Would be really nice to have const generics for this
    // Perhaps a U8SplitPair-like struct with byte[] and U8Range[] (or inline array) indices?
    internal static void Deconstruct<TSplit, TEnumerator>(this TSplit split, out U8String first, out U8String second)
        where TSplit : struct, IU8Enumerable<TEnumerator>
        where TEnumerator : struct, IU8Enumerator
    {
        // TODO: Should we throw on not match?
        (first, second) = (default , default);

        var enumerator = split.GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    internal static void Deconstruct<TSplit, TEnumerator>(
        this TSplit split,
        out U8String first,
        out U8String second,
        out U8String third)
            where TSplit : struct, IU8Enumerable<TEnumerator>
            where TEnumerator : struct, IU8Enumerator
    {
        (first, second, third) = (default, default, default);

        var enumerator = split.GetEnumerator();
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

    // TODO: Slower than string.Split, either remove or find a way to make this useful
    internal static int SplitRanges<T>(this U8String source, T separator, Span<U8Range> ranges)
    {
        var size = U8Info.GetSize(separator);
        var span = source.UnsafeSpan;
        var offset = source.Offset;
        var count = 0;
        var prev = 0;

        ref var ptr = ref ranges.AsRef();
        while (true)
        {
            var next = U8Searching.IndexOf(span.SliceUnsafe(prev), separator, size);
            if (next < 0)
            {
                break;
            }

            ptr.Add(count++) = new(prev + offset, next);
            prev += next + (int)size;
        }

        ptr.Add(count++) = new(prev + offset, span.Length - prev);
        return count;
    }
}
