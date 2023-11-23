using System.Buffers;
using System.Numerics;
using System.Text;

namespace U8Primitives;

public static class U8Info
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(ReadOnlySpan<byte> value)
    {
        if (value.Length is 1)
        {
            return IsAsciiByte(in value[0]);
        }

        return Ascii.IsValid(value);
    }

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
        return value is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;
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

            return Rune.DecodeFromUtf8(value, out var rune, out _) is OperationStatus.Done
                && Rune.IsWhiteSpace(rune);
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
        var b0 = ptr;
        var b1 = ptr.Add(1);

        // TODO: Consider switch expr. for nicer formatting and maybe better codegen?
        if (b0 is 0xC2 && (b1 is 0x85 or 0xA0))
        {
            size = 2;
            return true;
        }

        // If you are wondering why the formating is so weird or why instead of range checks there is
        // a bespoke pattern match - it's because this makes JIT/AOT produce better branch ordering
        // and more efficient range checks, and it kind of looks like a table which is easier on the eyes.
        // (in absolute terms, the codegen quality is still questionable but it's better than converting to rune)
        var b2 = ptr.Add(2);
        if ((b0 is 0xE1 && b1 is 0x9A && b2 is 0x80) ||
            (b0 is 0xE2 && (
                (b1 is 0x80 && b2 is 0x80 or 0x81 or 0x82 or 0x83 or 0x84 or 0x85 or 0x86 or 0x87 or 0x88 or 0x89 or 0x8A or 0xA8 or 0xA9 or 0xAF) ||
                (b1 is 0x81 && b2 is 0x9F))) ||
            (b0 is 0xE3 && b1 is 0x80 && b2 is 0x80))
        {
            size = 3;
            return true;
        }

        size = RuneLength(b0);
        return false;
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
