using System.Text;

namespace U8Primitives;

internal static class U8Info
{
    // TODO: Replace with lookup table (there will be a lot of them) and valid code, codegen for this sucks
    internal static bool IsWhitespaceSurrogate(byte b)
    {
        return b is (byte)' ' or 0x20 or 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x85 or 0xA0;
    }
}
