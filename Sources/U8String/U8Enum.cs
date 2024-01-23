using System.Collections.Frozen;
using System.Collections.Immutable;

namespace U8;

public static class U8Enum
{
    static class Lookup<T> where T : struct, Enum
    {
        public static readonly FrozenDictionary<U8String, T> Values =
            Enum.GetValues<T>().ToFrozenDictionary(ToU8String);
    }

    static class CaseInsensitiveLookup<T> where T : struct, Enum
    {
        // TODO: Switch to ordinal ignore case once it is implemented
        public static readonly FrozenDictionary<U8String, T> Values =
            Enum.GetValues<T>().ToFrozenDictionary(ToU8String, U8Comparison.AsciiIgnoreCase);
    }

    public static ImmutableArray<U8String> GetNames<T>() where T : struct, Enum
    {
        return Lookup<T>.Values.Keys;
    }

    public static ImmutableArray<T> GetValues<T>() where T : struct, Enum
    {
        return Lookup<T>.Values.Values;
    }

    public static T Parse<T>(U8String value) where T : struct, Enum
    {
        return Lookup<T>.Values[value];
    }

    public static T Parse<T>(U8String value, bool ignoreCase) where T : struct, Enum
    {
        return (ignoreCase ? CaseInsensitiveLookup<T>.Values  : Lookup<T>.Values)[value];
    }

    public static bool TryParse<T>(U8String value, out T result) where T : struct, Enum
    {
        return Lookup<T>.Values.TryGetValue(value, out result);
    }

    public static bool TryParse<T>(U8String value, bool ignoreCase, out T result) where T : struct, Enum
    {
        return (ignoreCase ? CaseInsensitiveLookup<T>.Values : Lookup<T>.Values)
            .TryGetValue(value, out result);
    }

    public static U8String ToU8String<T>(this T value) where T : struct, Enum
    {
        return new EnumU8StringFormat<T>(value).ToU8String();
    }
}
