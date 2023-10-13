using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

internal readonly struct U8Scalar
{
    internal readonly byte B0, B1, B2, B3;
    internal readonly byte Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Scalar(byte b)
    {
        B0 = b;
        Length = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Scalar(char c, [ConstantExpected] bool checkAscii = true)
    {
        Debug.Assert(!char.IsSurrogate(c));

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
