using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;

using U8.Abstractions;
using U8.Shared;

namespace U8;

#pragma warning disable RCS1003 // Use braces. Why: terseness
public static class U8Enum
{
    static class Lookup<T> where T : struct, Enum
    {
        public static readonly FrozenDictionary<T, U8String> Names =
            Enum.GetValues<T>().ToFrozenDictionary(key => key, ToU8String);
        public static readonly FrozenDictionary<U8String, T> Values =
            Enum.GetValues<T>().ToFrozenDictionary(ToU8String);
    }

    static class CaseInsensitiveLookup<T> where T : struct, Enum
    {
        // TODO: Switch to ordinal ignore case once it is implemented
        // TODO: This is slightly slower than Enum.Parse(..., ignoreCase: true), is it worth to special case it?
        public static readonly FrozenDictionary<U8String, T> Values =
            Enum.GetValues<T>().ToFrozenDictionary(ToU8String, U8Comparison.AsciiIgnoreCase);
    }

    public static U8EnumFormattable<T> AsFormattable<T>(T value) where T : struct, Enum
    {
        return new(value);
    }

    public static ImmutableArray<U8String> GetNames<T>() where T : struct, Enum
    {
        return Lookup<T>.Values.Keys;
    }

    public static ImmutableArray<T> GetValues<T>() where T : struct, Enum
    {
        return Lookup<T>.Values.Values;
    }

    // TODO: EH UX
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Parse<T>(U8String value) where T : struct, Enum
    {
        if (!Lookup<T>.Values.TryGetValue(value, out var result) &&
            !TryParseUnderlying(value, out result))
        {
            ThrowHelpers.ArgumentException();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Parse<T>(U8String value, bool ignoreCase) where T : struct, Enum
    {
        var lookup = ignoreCase ? CaseInsensitiveLookup<T>.Values : Lookup<T>.Values;

        if (!lookup.TryGetValue(value, out var result) &&
            !TryParseUnderlying(value, out result))
        {
            ThrowHelpers.ArgumentException();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryParse<T>(U8String value, out T result) where T : struct, Enum
    {
        return Lookup<T>.Values.TryGetValue(value, out result)
            || TryParseUnderlying(value, out result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryParse<T>(U8String value, bool ignoreCase, out T result) where T : struct, Enum
    {
        return (ignoreCase ? CaseInsensitiveLookup<T>.Values : Lookup<T>.Values)
            .TryGetValue(value, out result) || TryParseUnderlying(value, out result);
    }

    static bool TryParseUnderlying<T>(U8String value, out T result) where T : struct, Enum
    {
        result = default;

        if (!value.IsEmpty)
        {
            var span = value.UnsafeSpan;
            var type = typeof(T).GetEnumUnderlyingType();
            var culture = CultureInfo.InvariantCulture;

            if (type == typeof(byte)) return byte.TryParse(span, culture, out Unsafe.As<T, byte>(ref result));
            if (type == typeof(sbyte)) return sbyte.TryParse(span, culture, out Unsafe.As<T, sbyte>(ref result));
            if (type == typeof(short)) return short.TryParse(span, culture, out Unsafe.As<T, short>(ref result));
            if (type == typeof(ushort)) return ushort.TryParse(span, culture, out Unsafe.As<T, ushort>(ref result));
            if (type == typeof(int)) return int.TryParse(span, culture, out Unsafe.As<T, int>(ref result));
            if (type == typeof(uint)) return uint.TryParse(span, culture, out Unsafe.As<T, uint>(ref result));
            if (type == typeof(long)) return long.TryParse(span, culture, out Unsafe.As<T, long>(ref result));
            if (type == typeof(ulong)) return ulong.TryParse(span, culture, out Unsafe.As<T, ulong>(ref result));
            if (type == typeof(nint)) return nint.TryParse(span, culture, out Unsafe.As<T, nint>(ref result));
            if (type == typeof(nuint)) return nuint.TryParse(span, culture, out Unsafe.As<T, nuint>(ref result));
            // Cursed enum types
            if (type == typeof(float)) return float.TryParse(span, culture, out Unsafe.As<T, float>(ref result));
            if (type == typeof(double)) return double.TryParse(span, culture, out Unsafe.As<T, double>(ref result));
            if (type == typeof(char))
            {
                var rune = U8Conversions.CodepointToRune(ref span.AsRef(), out _);
                if (rune.IsBmp)
                {
                    result = Unsafe.BitCast<char, T>((char)rune.Value);
                    return true;
                }
            }
        }

        return false;
    }

    public static U8String ToU8String<T>(this T value) where T : struct, Enum
    {
        return new U8EnumFormattable<T>(value).ToU8String();
    }
}

public static class U8EnumExtensions
{
    public static U8EnumFormattable<T> AsU8Formattable<T>(this T value) where T : struct, Enum
    {
        return new(value);
    }
}

public readonly struct U8EnumFormattable<T> : IU8Formattable
    where T : notnull //, struct, Enum
{
    readonly static ConcurrentDictionary<T, byte[]> Cache = [];

    public T Value { get; }

    // This counter can be very imprecise but that's acceptable, we only
    // need to limit the cache size to some reasonable amount, tracking
    // the exact number of entries is not necessary.
    static uint Count;
    const int MaxCapacity = 2048;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8EnumFormattable(T value)
    {
        if (!typeof(T).IsValueType || !typeof(T).IsEnum)
        {
            throw new ArgumentException($"{typeof(T)} is not an enum type.", nameof(value));
        }

        Value = value;
    }

    public bool TryFormat(Span<byte> destination, out int bytesWritten)
    {
        var bytes = GetBytes(Value);
        var span = bytes.SliceUnsafe(0, bytes.Length - 1);
        if (destination.Length >= span.Length)
        {
            span.CopyToUnsafe(ref destination.AsRef());
            bytesWritten = span.Length;
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    public U8String ToU8String()
    {
        var bytes = GetBytes(Value);
        return new(bytes, bytes.Length - 1, neverEmpty: true);
    }

    public static implicit operator U8EnumFormattable<T>(T value) => new(value);
    public static implicit operator T(U8EnumFormattable<T> value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] GetBytes(T value)
    {
        return Cache.TryGetValue(value, out var cached) ? cached : Add(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] Add(T value)
    {
        var utf16 = value.ToString();
        var length = Encoding.UTF8.GetByteCount(utf16!);
        var count = Count;

        byte[] bytes;
        if (count <= MaxCapacity)
        {
            bytes = GC.AllocateArray<byte>(length + 1, pinned: true);
            Interlocked.Increment(ref Count);
        }
        else
        {
            bytes = new byte[length + 1];
        }

        Encoding.UTF8.GetBytes(utf16, bytes);

        if (count <= MaxCapacity)
        {
            Cache[value] = bytes;
        }

        return bytes;
    }

    U8String IU8Formattable.ToU8String(ReadOnlySpan<char> _, IFormatProvider? __)
    {
        return ToU8String();
    }

    bool IUtf8SpanFormattable.TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return TryFormat(utf8Destination, out bytesWritten);
    }
}
