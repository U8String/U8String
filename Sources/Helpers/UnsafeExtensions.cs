using System.Diagnostics;
using System.Runtime.InteropServices;

using U8.Primitives;

namespace U8;

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
        Debug.Assert(value != null);
        return ref MemoryMarshal.GetArrayDataReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this T[] value, int offset)
        where T : struct
    {
        Debug.Assert(value != null);
        Debug.Assert((uint)offset < (uint)value.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this T[] value, nint offset)
        where T : struct
    {
        Debug.Assert(value != null);
        Debug.Assert((uint)offset < (uint)value.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), offset);
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
    internal static ref T Subtract<T>(this ref T value, int offset)
        where T : struct
    {
        Debug.Assert(offset >= 0);
        return ref Unsafe.Subtract(ref value, (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref U Cast<T, U>(this ref T value)
        where T : unmanaged
        where U : unmanaged
    {
        return ref Unsafe.As<T, U>(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyToUnsafe<T>(this Span<T> source, ref T destination)
        where T : struct
    {
        source.CopyTo(MemoryMarshal.CreateSpan(ref destination, source.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CopyToUnsafe<T>(this ReadOnlySpan<T> source, ref T destination)
        where T : struct
    {
        source.CopyTo(MemoryMarshal.CreateSpan(ref destination, source.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int start)
        where T : struct
    {
        Debug.Assert(value != null);
        Debug.Assert((uint)start < (uint)value.Length);

        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this T[] value, int offset, int length)
        where T : struct
    {
        Debug.Assert(value != null);
        Debug.Assert((uint)offset + (uint)length <= (uint)value.Length);

        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> SliceUnsafe(this byte[] value, U8Range range)
    {
        Debug.Assert(value != null);
        Debug.Assert((uint)range.Offset + (uint)range.Length <= (uint)value.Length);

        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(value), (nint)(uint)range.Offset),
            range.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int start)
        where T : struct
    {
        Debug.Assert((uint)start < (uint)value.Length);

        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<T> SliceUnsafe<T>(this Span<T> value, int offset, int length)
        where T : struct
    {
        Debug.Assert((uint)offset + (uint)length <= (uint)value.Length);

        return MemoryMarshal.CreateSpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int start)
        where T : struct
    {
        Debug.Assert((uint)start < (uint)value.Length);

        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)start),
            value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<T> SliceUnsafe<T>(this ReadOnlySpan<T> value, int offset, int length)
        where T : struct
    {
        Debug.Assert((uint)offset + (uint)length <= (uint)value.Length);

        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset), length);
    }
}