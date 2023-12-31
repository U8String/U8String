using U8.Abstractions;
using U8.CaseConversion;

namespace U8;

public static class U8CaseConversion
{
    public static U8AsciiCaseConverter Ascii => default;

    // internal static U8FallbackInvariantCaseConverter FallbackInvariant => default;

    internal static bool IsTrustedImplementation<T>(T converter)
        where T : IU8CaseConverter
    {
        return converter is
            U8AsciiCaseConverter or
            U8FallbackInvariantCaseConverter;
    }
}
