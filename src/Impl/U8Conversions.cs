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
    {
        return ref MemoryMarshal.GetReference(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ref readonly T AsRef<T>(this ReadOnlySpan<T> value)
        where T : unmanaged
    {
        return ref MemoryMarshal.GetReference(value);
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
    internal static ReadOnlySpan<byte> ToUtf8<T>(T value, [UnscopedRef] out uint _)
    {
        Debug.Assert(value is byte or char or Rune or U8String or byte[]);

        _ = default;
        if (typeof(T).IsValueType)
        {
            var bytes = _.AsBytes();
            switch (value)
            {
                case byte b:
                    bytes.AsRef() = b;
                    return bytes.SliceUnsafe(0, 1);

                case char c:
                    if (char.IsAscii(c))
                    {
                        bytes.AsRef() = (byte)c;
                        return bytes.SliceUnsafe(0, 1);
                    }
                    else
                    {
                        return c.NonAsciiToUtf8(out _);
                    }

                case Rune r: return r.ToUtf8(out _);

                case U8String s: return s;

                default: return default;
            }
        }
        else
        {
            return Unsafe.As<T, byte[]>(ref value);
        }
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
