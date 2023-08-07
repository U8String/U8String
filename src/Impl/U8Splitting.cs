using System.Runtime.InteropServices;

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

    // TODO: Optimize?
    internal static void Deconstruct<TSplit, TEnumerator>(this TSplit split, out U8String first, out U8String second)
        where TSplit : IU8Split<TEnumerator>
        where TEnumerator : struct, IEnumerator<U8String>
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
            where TSplit : IU8Split<TEnumerator>
            where TEnumerator : struct, IEnumerator<U8String>
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
}
