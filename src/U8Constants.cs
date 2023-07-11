namespace U8Primitives;

internal static class U8Constants
{
    internal static ReadOnlySpan<byte> NewLineChars => "\n\r\f\u0085\u2028\u2029"u8;
    internal static ReadOnlySpan<byte> NewLineCharsExceptLF => "\r\f\u0085\u2028\u2029"u8;
}