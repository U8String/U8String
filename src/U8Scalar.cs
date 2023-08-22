using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

public struct U8Scalar
{
    internal byte B0, B1, B2, B3;
    internal byte Size;

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
            scalar.Size = 1;
            scalar.B0 = b;
        }
        else if (value is char c)
        {
            if (checkAscii && c <= 0x7F)
            {
                scalar.Size = 1;
                scalar.B0 = (byte)c;
            }
            else if (c <= 0x7FF)
            {
                scalar.Size = 2;
                scalar.B0 = (byte)(0xC0 | (c >> 6));
                scalar.B1 = (byte)(0x80 | (c & 0x3F));
            }
            else
            {
                scalar.Size = 3;
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
                scalar.Size = 1;
                scalar.B0 = (byte)r;
            }
            else if (r <= 0x7FF)
            {
                scalar.Size = 2;
                scalar.B0 = (byte)((r + (0b110u << 11)) >> 6);
                scalar.B1 = (byte)((r & 0x3Fu) + 0x80u);
            }
            else if (r <= 0xFFFF)
            {
                scalar.Size = 3;
                scalar.B0 = (byte)((r + (0b1110 << 16)) >> 12);
                scalar.B1 = (byte)(((r & (0x3Fu << 6)) >> 6) + 0x80u);
                scalar.B2 = (byte)((r & 0x3Fu) + 0x80u);
            }
            else
            {
                scalar.Size = 4;
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
    internal ReadOnlySpan<byte> AsSpan()
    {
        ref var src = ref this;
        return MemoryMarshal.CreateReadOnlySpan(ref src.B0, src.Size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(in U8Scalar scalar)
    {
        return scalar.AsSpan();
    }
}
