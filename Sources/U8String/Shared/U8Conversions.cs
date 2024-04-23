using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using U8.Primitives;

namespace U8.Shared;

internal static class U8Conversions
{
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
        Debug.Assert(U8Info.IsContinuationByte(b1));

        if (b0 >= 0xE0)
        {
            size = 3;
            var b2 = src.Add(2);
            var b1b2 = Accumulate((uint)(b1 & continuationMask), b2);
            rune = (init << 12) | b1b2;
            Debug.Assert(U8Info.IsContinuationByte(b2));

            if (b0 >= 0xF0)
            {
                size = 4;
                var b3 = src.Add(3);
                rune = ((init & 7) << 18) | Accumulate(b1b2, b3);
                Debug.Assert(U8Info.IsContinuationByte(b3));
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

    // TODO: This is a temporary slow implementation until
    // there is a port of Utf32to8 and Utf8to32 from simdutf.
    internal static U8String RunesToU8(ReadOnlySpan<Rune> runes)
    {
        var maxLength = (int)(uint)Math.Min((ulong)runes.Length * 4, (ulong)Array.MaxLength);
        var builder = new PooledU8Builder(maxLength);

        ref var ptr = ref builder.Free.AsRef();

        var written = 0;
        var remaining = builder.Free.Length;

        foreach (var rune in runes)
        {
            ref var dst = ref ptr.Add(written);
            switch (rune.Value, remaining)
            {
                case (<= 0x7F, >= 1):
                    dst = (byte)rune.Value;
                    written += 1;
                    remaining -= 1;
                    continue;
                case (<= 0x7FF, >= 2):
                    rune.AsTwoBytes().Store(ref dst);
                    written += 2;
                    remaining -= 2;
                    continue;
                case (<= 0xFFFF, >= 3):
                    rune.AsThreeBytes().Store(ref dst);
                    written += 3;
                    remaining -= 3;
                    continue;
                case (_, >= 4):
                    rune.AsFourBytes().Store(ref dst);
                    written += 4;
                    remaining -= 4;
                    continue;
                default:
                    ThrowHelpers.DestinationTooShort();
                    break;
            }
        }

        builder.BytesWritten = written;

        var result = new U8String(builder.Written, skipValidation: true);
        builder.Dispose();
        return result;
    }
}
