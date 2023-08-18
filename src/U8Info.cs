using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
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
        // .NET ARM64 supremacy: this is fused into cmp, ccmp and cinc.
        if (ArmBase.Arm64.IsSupported)
        {
            return value is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;
        }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWhitespaceRune(ReadOnlySpan<byte> value)
    {
        if (value.Length > 0)
        {
            var b = value[0];
            if (IsAsciiByte(b))
            {
                return IsAsciiWhitespace(b);
            }

            var res = Rune.DecodeFromUtf8(value, out var rune, out _);
            Debug.Assert(res is OperationStatus.Done);

            return Rune.IsWhiteSpace(rune);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsWhitespaceRune(ref byte ptr)
    {
        var b = ptr;
        return !IsContinuationByte(b) && IsAsciiByte(b)
            ? IsAsciiWhitespace(b)
            : IsNonAsciiWhitespace(ref ptr);
    }

    internal static bool IsNonAsciiWhitespace(ref byte ptr)
    {
        var value = MemoryMarshal.CreateSpan(ref ptr, 4);
        var res = Rune.DecodeFromUtf8(value, out var rune, out _);
        Debug.Assert(res is OperationStatus.Done);

        return Rune.IsWhiteSpace(rune);
    }

    // TODO: Is there really no better way to do this?
    // Why the hell does ARM64 have FJCVTZS but not something to count code point length?
    // TODO 2: Naming? Other options are ugly or long, or even more confusing.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RuneLength(in byte value)
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
