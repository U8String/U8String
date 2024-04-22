using System.Numerics;
using System.Text;

namespace U8;

public static class U8Info
{
    /// <summary>
    /// Determines wheter the bytes in <paramref name="value"/> comprise of ASCII characters only.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(ReadOnlySpan<byte> value)
    {
        return value.Length is 1
            ? IsAsciiByte(in value.AsRef())
            : Ascii.IsValid(value);
    }

    /// <summary>
    /// Determines wheter the provided byte is an ASCII character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiByte(in byte value)
    {
        return value <= 0x7F;
    }

    /// <summary>
    /// Determines wheter the provided byte is an ASCII letter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetter(in byte value)
    {
        return (uint)((value | 0x20) - 'a') <= 'z' - 'a';
    }

    /// <summary>
    /// Determines wheter the provided byte is an ASCII whitespace character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiWhitespace(in byte value)
    {
        // .NET ARM64 supremacy: this is fused into cmp, ccmp and cinc.
        return value is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;
    }

    /// <summary>
    /// Determines wheter the provided byte is a start of a UTF-8 code point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBoundaryByte(in byte value)
    {
        return (sbyte)value >= -64;
    }

    /// <summary>
    /// Determines wheter the provided byte is a continuation of a UTF-8 code point.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsContinuationByte(in byte value)
    {
        return (sbyte)value < -64;
    }

    /// <summary>
    /// Contract: input *must* be well-formed UTF-8.
    /// Will dereference past the end of the input if it's not.
    /// </summary>
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
        if (b0 < 0xE0)
        {
            size = 2;
            return b0 is 0xC2 && (b1 is 0x85 or 0xA0);
        }

        // If you are wondering why the formating is so weird or why instead of range checks there is
        // a bespoke pattern match - it's because this makes JIT/AOT produce better branch ordering
        // and more efficient range checks, and it kind of looks like a table which is easier on the eyes.
        // (in absolute terms, the codegen quality is still questionable but it's better than converting to rune)
        var b2 = ptr.Add(2);
        if (b0 < 0xF0)
        {
            size = 3;
            return b0 switch
            {
                0xE1 => ((b1 ^ 0x9A) | (b2 ^ 0x80)) is 0,
                0xE2 =>
                    (b1 is 0x80 && b2 is 0x80 or 0x81 or 0x82 or 0x83 or 0x84 or 0x85 or 0x86 or 0x87 or 0x88 or 0x89 or 0x8A or 0xA8 or 0xA9 or 0xAF) ||
                    ((b1 ^ 0x81) | (b2 ^ 0x9F)) is 0,
                0xE3 => ((b1 ^ 0x80) | (b2 ^ 0x80)) is 0,
                _ => false
            };
        }

        size = 4;
        return false;
    }

    /// <summary>
    /// Calculates the length of the UTF-8 code point starting at <paramref name="value"/>.
    /// </summary>
    /// <returns>
    /// If <paramref name="value"/> points at a start of a UTF-8 code point, the length of the
    /// code point in bytes; otherwise, <c>1</c>.
    /// <para />
    /// The behavior is undefined if <paramref name="value"/> points at a byte that
    /// is a part of a malformed UTF-8 byte sequence.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int RuneLength(in byte value)
    {
        // Do not use ternary here as it produces two jumps instead of one when inlined.
        // Otherwise, this is branchless. It may not be worth trying to optimize this further.
        // If you do, please update the hours spent tracker: 4h.
        var lzcnt = (uint)BitOperations.LeadingZeroCount(~((uint)value << 24));
        if (lzcnt is 0) lzcnt++;

        return (int)lzcnt;
    }
}
