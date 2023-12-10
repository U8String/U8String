using U8.Comparison;

namespace U8;

public static class U8Comparison
{
    public static U8OrdinalComparer Ordinal => default;
    // internal static U8OrdinalIgnoreCaseComparer OrdinalIgnoreCase => default;
    public static U8AsciiIgnoreCaseComparer AsciiIgnoreCase => default;
}
