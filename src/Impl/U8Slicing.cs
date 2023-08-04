using System.Runtime.InteropServices;

namespace U8Primitives;

static class U8Slicing
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int start)
        where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int offset, int length)
        where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int start)
        where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int offset, int length)
        where T : unmanaged
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int start)
        where T : unmanaged
    {
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int offset, int length)
        where T : unmanaged
    {
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }
}
