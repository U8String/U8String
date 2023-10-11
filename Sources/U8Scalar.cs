using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

internal struct U8Scalar
{
    internal byte B0, B1, B2, B3;
    internal byte Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8Scalar Create<T>(
        T value, [ConstantExpected] bool checkAscii = true) where T : unmanaged
    {
        Debug.Assert(value is byte or char or Rune or U8Scalar);

        if (value is U8Scalar self)
        {
            return self;
        }

        var scalar = new U8Scalar();
        if (value is byte b)
        {
            scalar.Length = 1;
            scalar.B0 = b;
        }
        else if (value is char c)
        {
            Debug.Assert(!char.IsSurrogate(c));

            if (checkAscii && c <= 0x7F)
            {
                Debug.Assert(char.IsAscii(c));

                scalar.Length = 1;
                scalar.B0 = (byte)c;
            }
            else if (c <= 0x7FF)
            {
                Debug.Assert(!char.IsAscii(c));

                scalar.Length = 2;
                scalar.B0 = (byte)(0xC0 | (c >> 6));
                scalar.B1 = (byte)(0x80 | (c & 0x3F));
            }
            else
            {
                scalar.Length = 3;
                scalar.B0 = (byte)(0xE0 | (c >> 12));
                scalar.B1 = (byte)(0x80 | ((c >> 6) & 0x3F));
                scalar.B2 = (byte)(0x80 | (c & 0x3F));
            }
        }
        else if (value is Rune rune)
        {
            var r = (uint)rune.Value;
            if (checkAscii && r <= 0x7F)
            {
                Debug.Assert(rune.IsAscii);
                Debug.Assert(rune.Utf8SequenceLength is 1);

                scalar.Length = 1;
                scalar.B0 = (byte)r;
            }
            else if (r <= 0x7FF)
            {
                Debug.Assert(rune.Utf8SequenceLength is 2);

                scalar.Length = 2;
                scalar.B0 = (byte)((r + (0b110u << 11)) >> 6);
                scalar.B1 = (byte)((r & 0x3Fu) + 0x80u);
            }
            else if (r <= 0xFFFF)
            {
                Debug.Assert(rune.Utf8SequenceLength is 3);

                scalar.Length = 3;
                scalar.B0 = (byte)((r + (0b1110 << 16)) >> 12);
                scalar.B1 = (byte)(((r & (0x3Fu << 6)) >> 6) + 0x80u);
                scalar.B2 = (byte)((r & 0x3Fu) + 0x80u);
            }
            else
            {
                Debug.Assert(rune.Utf8SequenceLength is 4);

                scalar.Length = 4;
                scalar.B0 = (byte)((r + (0b11110 << 21)) >> 18);
                scalar.B1 = (byte)(((r & (0x3Fu << 12)) >> 12) + 0x80u);
                scalar.B2 = (byte)(((r & (0x3Fu << 6)) >> 6) + 0x80u);
                scalar.B3 = (byte)((r & 0x3Fu) + 0x80u);
            }
        }
        else
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return scalar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8Scalar LoadUnsafe(ref byte ptr)
    {
        var scalar = new U8Scalar();
        ref var src = ref Unsafe.As<byte, uint>(ref ptr);
        ref var dst = ref Unsafe.As<byte, uint>(ref scalar.B0);
        dst = src;
        scalar.Length = (byte)(uint)U8Info.RuneLength(scalar.B0);

        return scalar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void StoreUnsafe<T>(ref T ptr) where T : unmanaged
    {
        Unsafe.As<T, uint>(ref ptr) = Unsafe.As<byte, uint>(ref B0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref B0, Length);
    }
}
