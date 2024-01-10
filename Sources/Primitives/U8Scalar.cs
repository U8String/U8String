using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8.Primitives;

readonly struct U8TwoBytes
{
    internal readonly ushort Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8TwoBytes(in byte ptr)
    {
        Value = Unsafe.AsRef(in ptr).Cast<byte, ushort>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8TwoBytes(char c)
    {
        Debug.Assert(!char.IsAscii(c));
        Debug.Assert(!char.IsSurrogate(c));

        Value = (ushort)
            (0xC0 | ((uint)c >> 6) |
            (0x80 | ((uint)c & 0x3F)) << 8);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8TwoBytes(Rune rune)
    {
        var r = (uint)rune.Value;

        Debug.Assert(!rune.IsAscii);
        Debug.Assert(r <= 0x7FF);

        Value = (ushort)(
            ((r + (0b110u << 11)) >> 6) |
            ((r & 0x3Fu) + 0x80u) << 8);
    }

    public void Store(ref byte ptr)
    {
        ptr.Cast<byte, ushort>() = Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in Value).Cast<ushort, byte>(), 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(in U8TwoBytes value)
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in value.Value).Cast<ushort, byte>(), 2);
    }
}

readonly struct U8ThreeBytes
{
    internal readonly uint Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8ThreeBytes(in byte ptr)
    {
        ref var src = ref Unsafe.AsRef(in ptr);

        var b01 = src.Cast<byte, ushort>();
        var b2 = src.Add(2).Cast<byte, byte>();

        Value = b01 | ((uint)b2 << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8ThreeBytes(char c)
    {
        Debug.Assert(!char.IsAscii(c));
        Debug.Assert(!char.IsSurrogate(c));

        Value =
            0xE0 | ((uint)c >> 12) |
            (0x80 | (((uint)c >> 6) & 0x3F)) << 8 |
            (0x80 | ((uint)c & 0x3F)) << 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8ThreeBytes(Rune rune)
    {
        var r = (uint)rune.Value;

        Debug.Assert(!rune.IsAscii);
        Debug.Assert(r is > 0x7FF and <= 0xFFFF);

        Value =
            ((r + (0b1110 << 16)) >> 12) |
            ((((r & (0x3Fu << 6)) >> 6) + 0x80u) << 8) |
            (((r & 0x3Fu) + 0x80u) << 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref byte ptr)
    {
        var value = Value;
        var b01 = (ushort)value;
        var b2 = (byte)(value >> 16);

        ptr.Cast<byte, ushort>() = b01;
        ptr.Add(2).Cast<byte, byte>() = b2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in Value).Cast<uint, byte>(), 3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(in U8ThreeBytes value)
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in value.Value).Cast<uint, byte>(), 3);
    }
}

readonly struct U8FourBytes
{
    internal readonly uint Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8FourBytes(in byte ptr)
    {
        Value = Unsafe.AsRef(in ptr).Cast<byte, uint>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8FourBytes(Rune rune)
    {
        var r = (uint)rune.Value;

        Debug.Assert(!rune.IsAscii);
        Debug.Assert(r is > 0xFFFF);

        Value =
            ((r + (0b11110 << 21)) >> 18) |
            ((((r & (0x3Fu << 12)) >> 12) + 0x80u) << 8) |
            ((((r & (0x3Fu << 6)) >> 6) + 0x80u) << 16) |
            (((r & 0x3Fu) + 0x80u) << 24);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(ref byte ptr)
    {
        ptr.Cast<byte, uint>() = Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in Value).Cast<uint, byte>(), 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(in U8FourBytes value)
    {
        return MemoryMarshal.CreateReadOnlySpan(
            in Unsafe.AsRef(in value.Value).Cast<uint, byte>(), 4);
    }
}

readonly struct U8Scalar
{
    internal readonly byte B0, B1, B2, B3;
    internal readonly int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Scalar(byte b)
    {
        Unsafe.SkipInit(out this);
        B0 = b;
        Length = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Scalar(char c, [ConstantExpected] bool checkAscii = true)
    {
        Debug.Assert(!char.IsSurrogate(c));

        Unsafe.SkipInit(out this);

        if (checkAscii && c <= 0x7F)
        {
            Debug.Assert(char.IsAscii(c));

            B0 = (byte)c;
            Length = 1;
        }
        else if (c <= 0x7FF)
        {
            B0 = (byte)(0xC0 | (c >> 6));
            B1 = (byte)(0x80 | (c & 0x3F));
            Length = 2;
        }
        else
        {
            B0 = (byte)(0xE0 | (c >> 12));
            B1 = (byte)(0x80 | ((c >> 6) & 0x3F));
            B2 = (byte)(0x80 | (c & 0x3F));
            Length = 3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Scalar(Rune rune, [ConstantExpected] bool checkAscii = true)
    {
        Unsafe.SkipInit(out this);

        var r = (uint)rune.Value;
        if (checkAscii && r <= 0x7F)
        {
            Debug.Assert(rune.IsAscii);
            Debug.Assert(rune.Utf8SequenceLength is 1);

            B0 = (byte)r;
            Length = 1;
        }
        else if (r <= 0x7FF)
        {
            Debug.Assert(rune.Utf8SequenceLength is 2);

            B0 = (byte)((r + (0b110u << 11)) >> 6);
            B1 = (byte)((r & 0x3Fu) + 0x80u);
            Length = 2;
        }
        else if (r <= 0xFFFF)
        {
            Debug.Assert(rune.Utf8SequenceLength is 3);

            B0 = (byte)((r + (0b1110 << 16)) >> 12);
            B1 = (byte)(((r & (0x3Fu << 6)) >> 6) + 0x80u);
            B2 = (byte)((r & 0x3Fu) + 0x80u);
            Length = 3;
        }
        else
        {
            Debug.Assert(rune.Utf8SequenceLength is 4);

            B0 = (byte)((r + (0b11110 << 21)) >> 18);
            B1 = (byte)(((r & (0x3Fu << 12)) >> 12) + 0x80u);
            B2 = (byte)(((r & (0x3Fu << 6)) >> 6) + 0x80u);
            B3 = (byte)((r & 0x3Fu) + 0x80u);
            Length = 4;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(in B0, Length);
    }
}
