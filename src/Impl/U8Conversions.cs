using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

internal static class U8Conversions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this Span<T> value)
        where T : struct
    {
        return ref MemoryMarshal.GetReference(value);
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
        return ref Unsafe.Add(ref value, (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T Add<T>(this ref T value, nuint offset)
        where T : struct
    {
        return ref Unsafe.Add(ref value, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Rune CodepointToRune(
        ref byte src,
        out int size,
        [ConstantExpected] bool checkAscii = true)
    {
        // from: https://github.com/rust-lang/rust/blob/master/library/core/src/str/validations.rs#L36
        const byte continuationMask = 0b0011_1111;

        var b0 = src;
        if (checkAscii && U8Info.IsAsciiByte(b0))
        {
            size = 1;
            return Unsafe.BitCast<uint, Rune>(b0);
        }

        size = 2;
        var init = FirstByte(b0, 2);
        var b1 = src.Add(1);
        var rune = Accumulate(init, b1);

        if (b0 >= 0xE0)
        {
            size = 3;
            var b2 = src.Add(2);
            var b1b2 = Accumulate((uint)(b1 & continuationMask), b2);
            rune = (init << 12) | b1b2;
            if (b0 >= 0xF0)
            {
                size = 4;
                var b3 = src.Add(3);
                rune = ((init & 7) << 18) | Accumulate(b1b2, b3);
            }
        }

        return Unsafe.BitCast<uint, Rune>(rune);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint FirstByte(byte value, int width)
        {
            return (uint)(value & (0x7F >> width));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Accumulate(uint rune, byte value)
        {
            return (rune << 6) | (uint)(value & continuationMask);
        }
    }
}
