using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using U8.Primitives;

namespace U8;

#pragma warning disable IDE1006, IDE0300, CA1825 // Pre-init arrays manually as described below
public static class U8Constants
{
    // Storing these as byte[] achieves two things:
    // - Enables pre-initialization on NativeAOT, removing cctor check
    // - Allows to do just a single mov/ldr and construct the string in place
    readonly static byte[] _crlf = [(byte)'\r', (byte)'\n', 0];
    readonly static byte[] _lf = [(byte)'\n', 0];
    readonly static byte[] _nullByte = new byte[1];
    readonly static byte[] _replacementChar = [239, 191, 189, 0];
    readonly static byte[] _asciiShrug = [194, 175, 92, 95, 40, 227, 131, 132, 41, 95, 47, 194, 175, 0];

    /// <summary>
    /// The directory separator character.
    /// </summary>
    /// <returns>'\' on Windows, '/' on Unix.</returns>
    public static byte DirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? (byte)'\\' : (byte)'/';
    }

    /// <summary>
    /// The newline separator string.
    /// </summary>
    /// <returns>"\r\n" on Windows, "\n" on Unix.</returns>
    public static U8String NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? new(_crlf, 0, 2) : new(_lf, 0, 1);
    }

    /// <summary>
    /// The null byte string.
    /// </summary>
    /// <returns>"\0"</returns>
    public static U8String NullByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_nullByte, 0, 1);
    }

    /// <summary>
    /// Replacement character for invalid UTF-8 sequences.
    /// </summary>
    /// <returns>'�'</returns>
    public static U8String ReplacementChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_replacementChar, 0, 3);
    }

    /// <summary>
    /// ¯\_(ツ)_/¯
    /// </summary>
    public static U8String AsciiShrug
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_asciiShrug, 0, 13);
    }

    internal readonly static byte[] EmptyBytes = new byte[0];
    internal readonly static char[] EmptyChars = new char[0];
    internal readonly static Rune[] EmptyRunes = new Rune[0];
    internal readonly static U8RuneIndex[] EmptyRuneIndices = new U8RuneIndex[0];

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
