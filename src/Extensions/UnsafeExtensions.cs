using System.Diagnostics;
using System.Runtime.InteropServices;

namespace U8Primitives;

internal static class UnsafeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this Span<T> value)
        where T : struct
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this ReadOnlySpan<T> value)
        where T : struct
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this ReadOnlySpan<T> value, int offset)
        where T : struct
    {
        Debug.Assert((uint)offset < (uint)value.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this T[] value)
        where T : struct
    {
        return ref MemoryMarshal.GetArrayDataReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this T[] value, int offset)
        where T : struct
    {
        Debug.Assert((uint)offset < (uint)value.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this Span<T> value, int offset)
        where T : struct
    {
        Debug.Assert((uint)offset < (uint)value.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Add<T>(this ref T value, int offset)
        where T : struct
    {
        Debug.Assert(offset >= 0);
        return ref Unsafe.Add(ref value, (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Add<T>(this ref T value, nuint offset)
        where T : struct
    {
        return ref Unsafe.Add(ref value, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyToUnsafe(this ReadOnlySpan<byte> source, ref byte destination)
    {
        // TODO: Is this better than source.CopyTo(destination.SliceUnsafe(0, source.Length))?
        // Unsafe.CopyBlock(
        //     destination: ref destination,
        //     source: ref MemoryMarshal.GetReference(source),
        //     byteCount: (uint)source.Length);

        source.CopyTo(MemoryMarshal.CreateSpan(ref destination, source.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int start)
        where T : struct
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int offset, int length)
        where T : struct
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int start)
        where T : struct
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int offset, int length)
        where T : struct
    {
        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int start)
        where T : struct
    {
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int offset, int length)
        where T : struct
    {
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }
}
