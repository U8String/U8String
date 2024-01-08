using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace U8;

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

    public static byte DirectorySeparator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? (byte)'\\' : (byte)'/';
    }

    public static U8String NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? new(_crlf, 0, 2) : new(_lf, 0, 1);
    }

    // This will be used in interop scenarios for empty strings.
    public static U8String NullByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_nullByte, 0, 1);
    }

    public static U8String ReplacementChar
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_replacementChar, 0, 3);
    }

    public static U8String AsciiShrug
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_asciiShrug, 0, 13);
    }
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
