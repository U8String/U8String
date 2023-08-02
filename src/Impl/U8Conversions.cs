using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

static class U8Conversions
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
    internal static ref T AsRef<T>(this Span<T> value)
        where T : unmanaged
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Offset<T>(this ref T value, nint offset)
        where T : unmanaged
    {
        return ref Unsafe.Add(ref value, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> ToUtf8(
        this Rune value,
        [UnscopedRef] out uint _)
    {
        _ = default;
        var bytes = _.AsBytes();
        var length = value.ToUtf8(bytes);

        return bytes.SliceUnsafe(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe int ToUtf8(this Rune value, Span<byte> destination)
    {
        fixed (byte* ptr = &MemoryMarshal.GetReference(destination))
        {
            var scalar = (uint)value.Value;

            // And I pray: unlimited optimization works
            // (dear compiler please fold this)
            if (scalar <= 0x7F)
            {
                ptr[0] = (byte)scalar;
                return 1;
            }
            else if (scalar <= 0x7FFu)
            {
                ptr[0] = (byte)((scalar + (0b110u << 11)) >> 6);
                ptr[1] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 2;
            }
            else if (scalar <= 0xFFFFu)
            {
                ptr[0] = (byte)((scalar + (0b1110 << 16)) >> 12);
                ptr[1] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[2] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 3;
            }
            else
            {
                ptr[0] = (byte)((scalar + (0b11110 << 21)) >> 18);
                ptr[1] = (byte)(((scalar & (0x3Fu << 12)) >> 12) + 0x80u);
                ptr[2] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[3] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 4;
            }
        }
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // internal static ReadOnlySpan<byte> ToUtf8<T>(this T value, [UnscopedRef] out uint _)
    // {
    //     _ = default;
    //     if (typeof(T).IsValueType)
    //     {
    //         var bytes = _.AsBytes();
    //         var length = 0;
    //         if (value is byte b)
    //         {
    //             bytes[0] = b;
    //             length = 1;
    //         }
    //         else if (value is char c)
    //         {
    //             length = new Rune(c).ToUtf8(bytes);
    //         }
    //         else if (value is Rune r)
    //         {
    //             length = r.ToUtf8(bytes);
    //         }
    //         else
    //         {
    //             ThrowHelpers.ArgumentOutOfRange();
    //         }

    //         return bytes.SliceUnsafe(0, length);
    //     }
    //     else
    //     {
    //         Debug.Assert(value is byte[]);
    //         return Unsafe.As<T, byte[]>(ref value!);
    //     }
    // }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe ReadOnlySpan<byte> NonAsciiToUtf8(this char value, [UnscopedRef] out uint _)
    {
        Debug.Assert(!char.IsAscii(value));

        _ = default;
        int length;
        var bytes = _.AsBytes();

        fixed (byte* ptr = &bytes.AsRef())
        {
            if (value <= 0x7FF)
            {
                ptr[0] = (byte)(0xC0 | (value >> 6));
                ptr[1] = (byte)(0x80 | (value & 0x3F));
                length = 2;
            }
            else
            {
                ptr[0] = (byte)(0xE0 | (value >> 12));
                ptr[1] = (byte)(0x80 | ((value >> 6) & 0x3F));
                ptr[2] = (byte)(0x80 | (value & 0x3F));
                length = 3;
            }
        }

        return bytes.SliceUnsafe(0, length);
    }
}
