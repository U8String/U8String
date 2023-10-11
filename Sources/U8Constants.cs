using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace U8Primitives;

public static class U8Constants
{
    public static byte DirectorySeparator =>
        OperatingSystem.IsWindows() ? (byte)'\\' : (byte)'/';

    public static U8String NewLine { get; } = new U8String(
        OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8, skipValidation: true);

    // This will be used in interop scenarios for empty strings.
    public static U8String NullByte { get; } = new U8String(new byte[1], 0, 1);

    public static U8String ReplacementChar { get; } = new U8String("�"u8, skipValidation: true);

    internal static long DefaultHashSeed { get; } = (long)GenerateSeed();

    private static ulong GenerateSeed()
    {
        var seed = 0ul;
        var span = MemoryMarshal.Cast<ulong, byte>(new Span<ulong>(ref seed));
        RandomNumberGenerator.Fill(span);
        return seed;
    }
}
