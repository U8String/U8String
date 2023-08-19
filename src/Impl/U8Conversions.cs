using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

internal static class U8Conversions
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
    internal static ReadOnlySpan<byte> AsReadOnlyBytes([UnscopedRef] this in uint value)
    {
        return new ReadOnlySpan<uint>(in value).AsBytes();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Span<byte> AsBytes<T>(this Span<T> value)
        where T : unmanaged
    {
        return MemoryMarshal.Cast<T, byte>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> value)
        where T : unmanaged
    {
        return MemoryMarshal.Cast<T, byte>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this Span<T> value)
        where T : struct
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref readonly T AsRef<T>(this ReadOnlySpan<T> value)
        where T : struct
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref T AsRef<T>(this Span<T> value, int offset)
        where T : struct
    {
        return ref Unsafe.Add(ref MemoryMarshal.GetReference(value), (nint)(uint)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref readonly T AsRef<T>(this ReadOnlySpan<T> value, int offset)
        where T : unmanaged
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int RuneToCodepoint(
        Rune rune,
        ref byte dst,
        [ConstantExpected] bool checkAscii = true)
    {
        var value = (uint)rune.Value;
        if (checkAscii && value <= 0x7F)
        {
            dst = (byte)value;
            return 1;
        }
        else if (value <= 0x7FFu)
        {
            dst.Add(0) = (byte)((value + (0b110u << 11)) >> 6);
            dst.Add(1) = (byte)((value & 0x3Fu) + 0x80u);
            return 2;
        }
        else if (value <= 0xFFFFu)
        {
            dst.Add(0) = (byte)((value + (0b1110 << 16)) >> 12);
            dst.Add(1) = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
            dst.Add(2) = (byte)((value & 0x3Fu) + 0x80u);
            return 3;
        }
        else
        {
            dst.Add(0) = (byte)((value + (0b11110 << 21)) >> 18);
            dst.Add(1) = (byte)(((value & (0x3Fu << 12)) >> 12) + 0x80u);
            dst.Add(2) = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
            dst.Add(3) = (byte)((value & 0x3Fu) + 0x80u);
            return 4;
        }
    }

    internal static int GetUtf8SequenceLength(Rune rune)
    {
        var value = (uint)rune.Value;
        var a = ((int)value - 0x0800) >> 31;

        // The number of UTF-8 code units for a given scalar is as follows:
        // - U+0000..U+007F => 1 code unit
        // - U+0080..U+07FF => 2 code units
        // - U+0800..U+FFFF => 3 code units
        // - U+10000+       => 4 code units
        //
        // If we XOR the incoming scalar with 0xF800, the chart mutates:
        // - U+0000..U+F7FF => 3 code units
        // - U+F800..U+F87F => 1 code unit
        // - U+F880..U+FFFF => 2 code units
        // - U+10000+       => 4 code units
        //
        // Since the 1- and 3-code unit cases are now clustered, they can
        // both be checked together very cheaply.

        value ^= 0xF800u;
        value -= 0xF880u;   // if scalar is 1 or 3 code units, high byte = 0xFF; else high byte = 0x00
        value += 4 << 24; // if scalar is 1 or 3 code units, high byte = 0x03; else high byte = 0x04
        value >>= 24;       // shift high byte down

        // Final return value:
        // - U+0000..U+007F => 3 + (-1) * 2 = 1
        // - U+0080..U+07FF => 4 + (-1) * 2 = 2
        // - U+0800..U+FFFF => 3 + ( 0) * 2 = 3
        // - U+10000+       => 4 + ( 0) * 2 = 4
        return (int)value + (a * 2);
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

    // TODO: Rewrite to a better codegen shape
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe int ToUtf8(this Rune value, Span<byte> destination)
    {
        fixed (byte* ptr = &destination.AsRef())
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe ReadOnlySpan<byte> NonAsciiToUtf8(
        this Rune value,
        [UnscopedRef] out uint _)
    {
        Debug.Assert(!value.IsAscii);

        _ = default;
        int length;
        var bytes = _.AsBytes();

        fixed (byte* ptr = &bytes.AsRef())
        {
            var scalar = (uint)value.Value;

            // And I pray: unlimited optimization works
            // (dear compiler please fold this)
            if (scalar <= 0x7FFu)
            {
                ptr[0] = (byte)((scalar + (0b110u << 11)) >> 6);
                ptr[1] = (byte)((scalar & 0x3Fu) + 0x80u);
                length = 2;
            }
            else if (scalar <= 0xFFFFu)
            {
                ptr[0] = (byte)((scalar + (0b1110 << 16)) >> 12);
                ptr[1] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[2] = (byte)((scalar & 0x3Fu) + 0x80u);
                length = 3;
            }
            else
            {
                ptr[0] = (byte)((scalar + (0b11110 << 21)) >> 18);
                ptr[1] = (byte)(((scalar & (0x3Fu << 12)) >> 12) + 0x80u);
                ptr[2] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[3] = (byte)((scalar & 0x3Fu) + 0x80u);
                length = 4;
            }

            return bytes.SliceUnsafe(0, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe ReadOnlySpan<byte> NonAsciiToUtf8(this char value, [UnscopedRef] out uint _)
    {
        Debug.Assert(!char.IsAscii(value));
        Debug.Assert(!char.IsSurrogate(value));

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
