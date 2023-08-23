using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace U8Primitives;

public static class U8Constants
{
    public static SearchValues<byte> AsciiWhitespace { get; } =
        SearchValues.Create("\t\n\v\f\r "u8);

    public static U8String NewLine { get; } = new U8String(
        OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8, skipValidation: true);

    public static byte DirectorySeparator =>
        OperatingSystem.IsWindows() ? (byte)'\\' : (byte)'/';

    internal static long DefaultHashSeed { get; } = (long)GenerateSeed();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetFormattedLength<T>(T value) => value switch
    {
        byte => 8,
        char => 8,
        Rune => 8,
        sbyte => 8,
        ushort => 8,
        short => 8,
        uint => 16,
        int => 16,
        ulong => 24,
        long => 24,
        float => 16,
        double => 24,
        decimal => 32,
        DateTime => 32,
        DateTimeOffset => 40,
        TimeSpan => 24,
        Guid => 40,
        _ => 32,
    };

    private static ulong GenerateSeed()
    {
        var seed = 0ul;
        RandomNumberGenerator.Fill(seed.AsBytes());
        return seed;
    }
}
