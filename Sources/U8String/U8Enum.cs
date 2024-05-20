using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using U8.Abstractions;
using U8.Shared;

namespace U8;

public static class U8Enum
{
    internal static class EnumInfo<T> where T : struct, Enum
    {
        internal static readonly T[] Values = Enum.GetValues<T>();

        internal static readonly U8String[] Names = GetNames();

        internal static readonly bool IsContiguousFromZero = CheckContiguousFromZero();

        internal static readonly FrozenDictionary<T, ByteArray> NameLookup =
            Zip(Values, Names, (v, n) => KeyValuePair.Create(v, new ByteArray(n._value!))).ToFrozenDictionary();

        internal static readonly FrozenDictionary<U8String, T> ValueLookup =
            Zip(Values, Names, (v, n) => KeyValuePair.Create(n, v)).ToFrozenDictionary();

        internal static readonly FrozenDictionary<U8String, T> CaseInsensitiveValueLookup =
            ValueLookup.ToFrozenDictionary(U8Comparison.AsciiIgnoreCase);

        static IEnumerable<U> Zip<T1, T2, U>(T1[] first, T2[] second, Func<T1, T2, U> selector)
        {
            var length = Math.Min(first.Length, second.Length);
            for (var i = 0; i < length; i++)
            {
                yield return selector(first[i], second[i]);
            }
        }

        static U8String[] GetNames()
        {
            var values = Values;
            var namesUtf16 = Enum.GetNames<T>();
            var names = new U8String[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                var nameBytes = GetAndCacheNameBytes(values[i], namesUtf16[i]);
                names[i] = new U8String(nameBytes, 0, nameBytes.Length - 1);
            }

            return names;
        }

        static byte[] GetAndCacheNameBytes(T value, string name)
        {
            var length = Encoding.UTF8.GetByteCount(name);
            var bytes = GC.AllocateArray<byte>(length + 1, pinned: true);
            if (bytes.Length is 1)
            {
                throw new ArgumentException("Enum has a member with an empty name. This is not supported.");
            }
            Encoding.UTF8.GetBytes(name, bytes);
            // Make sure to deduplicate the cached enum name literals, overwrite the existing entry if it exists
            return U8EnumFormattable<T>.Cache[value] = bytes;
        }

        static bool CheckContiguousFromZero()
        {
            var type = typeof(T).GetEnumUnderlyingType();
            if (type == typeof(byte)) return CheckUnderlying<byte>();
            if (type == typeof(sbyte)) return CheckUnderlying<sbyte>();
            if (type == typeof(short)) return CheckUnderlying<short>();
            if (type == typeof(ushort)) return CheckUnderlying<ushort>();
            if (type == typeof(int)) return CheckUnderlying<int>();
            if (type == typeof(uint)) return CheckUnderlying<uint>();
            if (type == typeof(long)) return CheckUnderlying<long>();
            if (type == typeof(ulong)) return CheckUnderlying<ulong>();
            if (type == typeof(nint)) return CheckUnderlying<nint>();
            if (type == typeof(nuint)) return CheckUnderlying<nuint>();

            return false;

            static bool CheckUnderlying<U>() where U : struct, IBinaryInteger<U>
            {
                var values = Values;
                var expected = U.Zero;
                foreach (var value in values)
                {
                    var underlying = Unsafe.BitCast<T, U>(value);
                    if (underlying != expected)
                    {
                        return false;
                    }

                    expected++;
                }

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetNameContiguous(T value, out U8String name)
        {
            Unsafe.SkipInit(out name);
            Debug.Assert(IsContiguousFromZero);

            var index = 0;
            var names = Names;
            var type = typeof(T).GetEnumUnderlyingType();

            if (type == typeof(byte)) index = Unsafe.As<T, byte>(ref value);
            if (type == typeof(sbyte)) index = Unsafe.As<T, sbyte>(ref value);
            if (type == typeof(short)) index = Unsafe.As<T, short>(ref value);
            if (type == typeof(ushort)) index = Unsafe.As<T, ushort>(ref value);
            if (type == typeof(int)) index = Unsafe.As<T, int>(ref value);
            if (type == typeof(uint)) index = (int)Unsafe.As<T, uint>(ref value);
            if (type == typeof(long)) index = (int)Unsafe.As<T, long>(ref value);
            if (type == typeof(ulong)) index = (int)Unsafe.As<T, ulong>(ref value);
            if (type == typeof(nint)) index = (int)Unsafe.As<T, nint>(ref value);
            if (type == typeof(nuint)) index = (int)Unsafe.As<T, nuint>(ref value);

            if ((uint)index < (uint)names.Length)
            {
                name = names.AsRef(index);
                return true;
            }

            return false;
        }
    }

    public static U8EnumFormattable<T> AsFormattable<T>(T value) where T : struct, Enum
    {
        return new(value);
    }

    public static U8String? GetName<T>(T value) where T : struct, Enum
    {
        if (EnumInfo<T>.IsContiguousFromZero)
        {
            if (EnumInfo<T>.TryGetNameContiguous(value, out var name))
            {
                return name;
            }
        }
        else if (EnumInfo<T>.NameLookup.TryGetValue(value, out var bytes))
        {
            var array = bytes.Array;
            return new(array, array.Length - 1, neverEmpty: true);
        }

        return null;
    }

    public static ImmutableArray<U8String> GetNames<T>() where T : struct, Enum
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(EnumInfo<T>.Names);
    }

    public static ImmutableArray<T> GetValues<T>() where T : struct, Enum
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(EnumInfo<T>.Values);
    }

    // TODO: EH UX
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Parse<T>(U8String value) where T : struct, Enum
    {
        if (!EnumInfo<T>.ValueLookup.TryGetValue(value, out var result))
        {
            ThrowHelpers.ArgumentException();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Parse<T>(U8String value, bool ignoreCase) where T : struct, Enum
    {
        var lookup = ignoreCase
            ? EnumInfo<T>.CaseInsensitiveValueLookup
            : EnumInfo<T>.ValueLookup;

        if (!lookup.TryGetValue(value, out var result))
        {
            ThrowHelpers.ArgumentException();
        }

        return result;
    }

    public static T Parse<T>(U8String value, U8EnumParseOptions options) where T : struct, Enum
    {
        var lookup = options.HasFlag(U8EnumParseOptions.IgnoreCase)
            ? EnumInfo<T>.CaseInsensitiveValueLookup
            : EnumInfo<T>.ValueLookup;

        var numeric = options.HasFlag(U8EnumParseOptions.AllowNumericValues);

        if (!lookup.TryGetValue(value, out var result) && (
            !numeric || !TryParseUnderlying(value, out result)))
        {
            ThrowHelpers.ArgumentException();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryParse<T>(U8String value, out T result) where T : struct, Enum
    {
        return EnumInfo<T>.ValueLookup.TryGetValue(value, out result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool TryParse<T>(U8String value, bool ignoreCase, out T result) where T : struct, Enum
    {
        return (ignoreCase ? EnumInfo<T>.CaseInsensitiveValueLookup : EnumInfo<T>.ValueLookup)
            .TryGetValue(value, out result);
    }

    public static bool TryParse<T>(U8String value, U8EnumParseOptions options, out T result) where T : struct, Enum
    {
        var lookup = options.HasFlag(U8EnumParseOptions.IgnoreCase)
            ? EnumInfo<T>.CaseInsensitiveValueLookup
            : EnumInfo<T>.ValueLookup;

        var numeric = options.HasFlag(U8EnumParseOptions.AllowNumericValues);

        return lookup.TryGetValue(value, out result) || (numeric && TryParseUnderlying(value, out result));
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
}

[Flags]
public enum U8EnumParseOptions
{
    None = 0,
    IgnoreCase = 1,
    AllowNumericValues = 2,
}

public static class U8EnumExtensions
{
    public static U8EnumFormattable<T> AsU8Formattable<T>(this T value) where T : struct, Enum
    {
        return new(value);
    }

    public static U8String ToU8String<T>(this T value) where T : struct, Enum
    {
        // See U8EnumFormattable<T> for the rationale behind this implementation
        if (U8Enum.EnumInfo<T>.IsContiguousFromZero)
        {
            if (U8Enum.EnumInfo<T>.TryGetNameContiguous(value, out var name))
            {
                return name;
            }
        }
        else if (U8Enum.EnumInfo<T>.NameLookup.TryGetValue(value, out var bytes))
        {
            var array = bytes.Array;
            return new(array, array.Length - 1, neverEmpty: true);
        }

        return U8EnumFormattable<T>.ToU8StringUndefined(value);
    }
}

// Unfortunately, this struct will partially duplicate formatting logic from U8Enum
// because the "bridging generic constraints" feature is still work in progress and
// we must use unconstrained generic argument due to method overload resulution characteristics
// affecting InterpolatedU8StringHandler which requires its AppendFormatted<T> to be unconstrained.
public readonly struct U8EnumFormattable<T> : IU8Formattable
    where T : notnull //, struct, Enum
{
    // This cache mixes both defined and undefined enum values.
    // Defined values are cached unconditionally, while undefined ones are subject to UndefinedItemLimit.
    internal static readonly ConcurrentDictionary<T, ByteArray> Cache = [];
    internal static readonly bool IsFlags = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);

    static int UndefinedItemCount;
    const int UndefinedItemLimit = 4096;

    public T Value { get; }

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
        var utf8 = ToU8String();
        if (destination.Length >= utf8.Length)
        {
            utf8.UnsafeSpan.CopyToUnsafe(ref destination.AsRef());
            bytesWritten = utf8.Length;
            return true;
        }

        bytesWritten = 0;
        return false;
    }

    public U8String ToU8String()
    {
        if (Cache.TryGetValue(Value, out var bytes))
        {
            var array = bytes.Array;
            return new(array, array.Length - 1, neverEmpty: true);
        }

        return !IsFlags ? FormatNew(Value) : FormatFlags(Value);
    }

    internal static U8String ToU8StringUndefined(T value)
    {
        if (Cache.TryGetValue(value, out var bytes))
        {
            var array = bytes.Array;
            return new(array, array.Length - 1, neverEmpty: true);
        }

        return !IsFlags ? FormatUnderlying(value) : FormatFlags(value);
    }

    public static implicit operator U8EnumFormattable<T>(T value) => new(value);
    public static implicit operator T(U8EnumFormattable<T> value) => value.Value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static U8String FormatNew(T value)
    {
        Debug.Assert(!IsFlags);
        if (!typeof(T).IsValueType || !typeof(T).IsEnum)
        {
            throw new ArgumentException($"{typeof(T)} is not an enum type.", nameof(value));
        }

        var utf16 = Enum.GetName(typeof(T), value);
        if (utf16 != null)
        {
            Debug.Assert(utf16.Length > 0);
            // Allocate name literal on the regular heap to avoid pinned object heap fragmentation
            // in case it is overwritten by an initializer inside U8Enum or this method loses the
            // race to add the name to the cache.
            var length = Encoding.UTF8.GetByteCount(utf16);
            var bytes = new byte[length + 1];

            Encoding.UTF8.GetBytes(utf16, bytes);
            return new(Cache.GetOrAdd(value, bytes), length, neverEmpty: true);
        }

        return FormatUnderlying(value);
    }

    static U8String FormatUnderlying(T value)
    {
        Debug.Assert(!IsFlags);
        Unsafe.SkipInit(out U8String formatted);
        var type = typeof(T).GetEnumUnderlyingType();

        if (type == typeof(byte)) formatted = U8String.Create(Unsafe.As<T, byte>(ref value));
        else if (type == typeof(sbyte)) formatted = U8String.Create(Unsafe.As<T, sbyte>(ref value));
        else if (type == typeof(short)) formatted = U8String.Create(Unsafe.As<T, short>(ref value));
        else if (type == typeof(ushort)) formatted = U8String.Create(Unsafe.As<T, ushort>(ref value));
        else if (type == typeof(int)) formatted = U8String.Create(Unsafe.As<T, int>(ref value));
        else if (type == typeof(uint)) formatted = U8String.Create(Unsafe.As<T, uint>(ref value));
        else if (type == typeof(long)) formatted = U8String.Create(Unsafe.As<T, long>(ref value));
        else if (type == typeof(ulong)) formatted = U8String.Create(Unsafe.As<T, ulong>(ref value));
        else if (type == typeof(nint)) formatted = U8String.Create(Unsafe.As<T, nint>(ref value));
        else if (type == typeof(nuint)) formatted = U8String.Create(Unsafe.As<T, nuint>(ref value));
        // Cursed enum types
        else if (type == typeof(float)) formatted = U8String.Create(Unsafe.As<T, float>(ref value));
        else if (type == typeof(double)) formatted = U8String.Create(Unsafe.As<T, double>(ref value));
        else if (type == typeof(char)) formatted = U8String.Create(Unsafe.As<T, char>(ref value));
        else ThrowHelpers.Unreachable();

        var itemCount = UndefinedItemCount;
        if (itemCount < UndefinedItemLimit)
        {
            Interlocked.Increment(ref UndefinedItemCount);
            // Null-terminate to match the exact form of bytes and bytes.Length - 1
            var bytes = Cache.GetOrAdd(value, formatted.Clone()._value!).Array;
            formatted = new(bytes, bytes.Length - 1, neverEmpty: true);
        }

        return formatted;
    }

    // This implementation looks like a lazy shortcut, but it isn't.
    // There seems to be no way to retrieve the enum names/values from Enum class
    // without either generic constraints bridging (in some future C# version)
    // or without producing code that is problematic for AOT compilation.
    // See comment at the top of this type for more details.
    static U8String FormatFlags(T value)
    {
        Debug.Assert(IsFlags);

        var utf16 = value.ToString() ?? ThrowHelpers.Unreachable<string>();
        Debug.Assert(utf16.Length > 0);
        var length = Encoding.UTF8.GetByteCount(utf16);
        var bytes = new byte[length + 1];

        Encoding.UTF8.GetBytes(utf16, bytes);

        var itemCount = UndefinedItemCount;
        if (itemCount < UndefinedItemLimit)
        {
            Interlocked.Increment(ref UndefinedItemCount);
            bytes = Cache.GetOrAdd(value, bytes).Array;
        }

        return new(bytes, length, neverEmpty: true);
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
