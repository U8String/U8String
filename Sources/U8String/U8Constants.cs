using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using U8.Primitives;

namespace U8;

#pragma warning disable IDE1006, IDE0300, CA1825 // Pre-init arrays manually as described below
public static class U8Constants
{
    /// <summary>
    /// The byte order mark for UTF-8.
    /// </summary>
    /// <returns>0xEF, 0xBB, 0xBF which is the Unicode code point U+FEFF.</returns>
    public static U8String ByteOrderMark => u8('\uFEFF');

    /// <summary>
    /// The directory separator character.
    /// </summary>
    /// <returns>"\" on Windows, "/" on Unix or otherwise.</returns>
    public static byte DirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? (byte)'\\' : (byte)'/';
    }

    /// <summary>
    /// The newline separator string.
    /// </summary>
    /// <returns>"\r\n" on Windows, "\n" on Unix or otherwise.</returns>
    public static U8String NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? u8("\r\n") : u8("\n");
    }

    /// <summary>
    /// The null byte string.
    /// </summary>
    /// <returns>"\0"</returns>
    public static U8String NullByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => u8("\0");
    }

    /// <summary>
    /// Replacement character for invalid UTF-8 sequences.
    /// </summary>
    /// <returns>"�"</returns>
    public static U8String ReplacementChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => u8("�");
    }

    /// <summary>
    /// ¯\_(ツ)_/¯
    /// </summary>
    public static U8String AsciiShrug
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => u8("¯\\_(ツ)_/¯");
    }

    internal readonly static byte[] EmptyBytes = new byte[0];
    internal readonly static char[] EmptyChars = new char[0];
    internal readonly static Rune[] EmptyRunes = new Rune[0];
    internal readonly static U8RuneIndex[] EmptyRuneIndices = new U8RuneIndex[0];
    internal readonly static U8Range[] EmptyRanges = new U8Range[0];

    // Zero-length arrays of non-primitive type are pre-initialized too.
    internal readonly static U8String[] EmptyStrings = new U8String[0];
}

static class U8HashSeed
{
    internal readonly static long Value = (long)Generate();

    static ulong Generate()
    {
        var seed = 0ul;
        var span = MemoryMarshal.Cast<ulong, byte>(new Span<ulong>(ref seed));
        RandomNumberGenerator.Fill(span);
        return seed;
    }
}
