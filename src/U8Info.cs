using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace U8Primitives;

public static class U8Info
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiByte(in byte value)
    {
        return value <= 0x7F;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiWhitespace(in byte value)
    {
        // On ARM64, .NET does this quite well actually with cmp, ccmp+ccmp and cinc.
        // I do wonder if there's a better way to do this though.
        // ---- Ver. 1 -----------------------------------
        // return value is 0x20 or (>= 0x09 and <= 0x0D);
        // ---- Ver. 2 -----------------------------------
        // const ulong mask = 4294983168;
        // var w0 = (uint)value & 255;
        // var x1 = w0 < 33 ? 1ul : 0ul;
        // var x2 = mask >> (int)w0;
        // var res = x1 & x2;
        // return Unsafe.As<ulong, bool>(ref res);
        // ---- Ver. 3 -----------------------------------
        // Adopted from codegen LLVM emits for Rust's char::is_ascii_whitespace
        const ulong mask = 4294983168;
        var x1 = (uint)value < 33 ? 1ul : 0ul;
        var x2 = mask >> value;
        var res = x1 & x2;
        return Unsafe.As<ulong, bool>(ref res);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsContinuationByte(in byte value)
    {
        return (sbyte)value < -64;
    }

    // TODO: Is there really no better way to do this?
    // Why the hell does ARM64 have FCVTZS but not something to count code point length?
    // TODO 2: Naming? Other options are ugly or long, or even more confusing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CharLength(in byte value)
    {
        var lzcnt = BitOperations.LeadingZeroCount(~(uint)(value << 24));
        var flag = IsAsciiByte(value);

        return flag ? 1 : lzcnt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8Size GetSize<T>(T value)
    {
        Debug.Assert(value is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return value switch
            {
                byte => U8Size.Ascii,
                char c => c switch
                {
                    <= (char)0x7F => U8Size.Ascii,
                    <= (char)0x7FF => U8Size.Two,
                    _ => U8Size.Three
                },
                Rune r => r.Value switch
                {
                    <= 0x7F => U8Size.Ascii,
                    <= 0x7FF => U8Size.Two,
                    <= 0xFFFF => U8Size.Three,
                    _ => U8Size.Four
                },
                U8String str => (U8Size)str.Length,
                _ => 0
            };
        }
        else
        {
            return (U8Size)Unsafe.As<byte[]>(value).Length;
        }
    }
}
