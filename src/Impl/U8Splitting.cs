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

    // TODO: Slower than string.Split, either remove or find a way to make this useful
    internal static int SplitRanges<T>(
        // Change this to Span<int> indices if I ever decide to pursue this variant
        this U8String source, T separator, Span<U8Range> ranges) where T : unmanaged
    {
        var scalar = U8Scalar.Create(separator);
        var span = source.UnsafeSpan;
        var offset = source.Offset;
        var count = 0;
        var prev = 0;

        ref var ptr = ref ranges.AsRef();
        while (true)
        {
            var next = U8Searching.IndexOf(span.SliceUnsafe(prev), scalar);
            if (next < 0)
            {
                break;
            }

            ptr.Add(count++) = new(prev + offset, next);
            prev += next + scalar.Size;
        }

        ptr.Add(count++) = new(prev + offset, span.Length - prev);
        return count;
    }
}
