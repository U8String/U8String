using System.Buffers;
using System.Diagnostics;
using System.Numerics;
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
    public static bool IsAsciiLetter(in byte value)
    {
        return (uint)((value | 0x20) - 'a') <= 'z' - 'a';
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
    internal static bool IsWhitespaceRune(ref byte ptr, out int size)
    {
        var b = ptr;
        if (IsAsciiByte(b))
        {
            size = 1;
            return IsAsciiWhitespace(b);
        }

        return IsNonAsciiWhitespace(ref ptr, out size);
    }

    internal static bool IsNonAsciiWhitespace(ref byte ptr, out int size)
    {
        var rune = U8Conversions.CodepointToRune(ref ptr, out size, checkAscii: false);
        Debug.Assert(Rune.IsValid(rune.Value));

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
}
