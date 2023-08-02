using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

static class Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsEmpty(this Range value)
    {
        var (start, end) = Unsafe.As<Range, (int, int)>(ref value);
        return start == end;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> AsBytes<T>([UnscopedRef] this ref T value)
        where T : unmanaged
    {
        return new Span<T>(ref value).AsBytes();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> AsBytes<T>(this Span<T> value)
        where T : unmanaged
    {
        return MemoryMarshal.Cast<T, byte>(value);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> ToUtf8Span<T>(this T value, [UnscopedRef] out uint _)
    {
        _ = default;
        if (typeof(T).IsValueType)
        {
            var bytes = _.AsBytes();
            var length = 0;
            if (value is byte b)
            {
                bytes[0] = b;
                length = 1;
            }
            else if (value is char c)
            {
                length = new Rune(c).ToUtf8Unsafe(bytes);
            }
            else if (value is Rune r)
            {
                length = r.ToUtf8Unsafe(bytes);
            }
            else
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return bytes.SliceUnsafe(0, length);
        }
        else
        {
            Debug.Assert(value is byte[]);
            return Unsafe.As<T, byte[]>(ref value!);
        }
    }
}
