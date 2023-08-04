using System.Security.Cryptography;
using System.Text;

namespace U8Primitives;

static class U8Constants
{
    internal static ReadOnlySpan<byte> NewLineChars => "\r\n\f\u0085\u2028\u2029"u8;
    internal static ReadOnlySpan<byte> NewLineCharsExceptLF => "\r\f\u0085\u2028\u2029"u8;

    internal static long DefaultHashSeed { get; } = (long)GenerateSeed();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetFormattedLength<T>() => typeof(T) switch
    {
        _ when typeof(T) == typeof(sbyte) => 4,
        _ when typeof(T) == typeof(short) => 6,
        _ when typeof(T) == typeof(char) => 4,
        _ when typeof(T) == typeof(Rune) => 4,
        _ when typeof(T) == typeof(int) => 11,
        _ when typeof(T) == typeof(long) => 20,
        _ when typeof(T) == typeof(ushort) => 5,
        _ when typeof(T) == typeof(uint) => 10,
        _ when typeof(T) == typeof(ulong) => 20,
        _ when typeof(T) == typeof(float) => 11,
        _ when typeof(T) == typeof(double) => 20,
        _ when typeof(T) == typeof(decimal) => 29,
        _ when typeof(T) == typeof(DateTime) => 29,
        _ when typeof(T) == typeof(DateTimeOffset) => 39,
        _ when typeof(T) == typeof(TimeSpan) => 24,
        _ when typeof(T) == typeof(Guid) => 36,
        _ => 32,
    };

    private static ulong GenerateSeed()
    {
        var seed = 0ul;
        RandomNumberGenerator.Fill(seed.AsBytes());
        return seed;
    }
}
