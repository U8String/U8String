namespace U8Primitives;

internal static class U8Info
{
    // TODO: Replace with lookup table (there will be a lot of them) and valid code, codegen for this sucks
    internal static bool IsWhitespaceSurrogate(byte b)
    {
        return b is (byte)' ' or 0x20 or 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x85 or 0xA0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsContinuation(byte b)
    {
        // A continuation byte has the form 10xxxxxx
        // So we can check if the most significant bit is 1 and the second most significant bit is 0
        return (b & 0xC0) == 0x80;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsStart(byte b)
    {
        // A start byte has the form 0xxxxxxx, 110xxxxx, 1110xxxx, or 11110xxx
        // So we can check if the most significant bit is 0 or the second most significant bit is 1
        return (b & 0x80) == 0 || (b & 0x40) == 0x40;
    }
}
