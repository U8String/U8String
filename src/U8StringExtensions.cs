using System.Collections.Immutable;
using U8Primitives;

namespace System;

#pragma warning disable CA1305 // Specify IFormatProvider
/// <summary>
/// Provides extension methods to integrate <see cref="U8String"/> with the .NET type system.
/// </summary>
public static class U8StringExtensions
{
    /// <inheritdoc cref="U8String(ImmutableArray{byte})"/>
    public static U8String AsU8String(this ImmutableArray<byte> value) => new(value);

    /// <inheritdoc cref="U8String(string?)"/>
    public static U8String ToU8String(this string? value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    public static U8String ToU8String(this byte[] value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    public static U8String ToU8String(this Span<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    public static U8String ToU8String(this Span<char> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    public static U8String ToU8String(this ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    public static U8String ToU8String(this ReadOnlySpan<char> value) => new(value);

    /// <summary>
    /// Converts the <see paramref="value"/> to a <see cref="U8String"/> using the default format.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>U8String representation of the value.</returns>
    public static U8String ToU8String<T>(this T value)
        where T : IUtf8SpanFormattable
    {
        return ToU8String(value, default, null);
    }

    /// <summary>
    /// Converts the <see paramref="value"/> to a <see cref="U8String"/> using the specified format.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format to use.</param>
    /// <returns>U8String representation of the value.</returns>
    public static U8String ToU8String<T>(this T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
        return ToU8String(value, format, null);
    }

    /// <summary>
    /// Converts the <see paramref="value"/> to a <see cref="U8String"/> using the default format and a provider, if specified.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="provider">The format provider to use.</param>
    /// <returns>U8String representation of the value.</returns>
    public static U8String ToU8String<T>(this T value, IFormatProvider? provider)
        where T : IUtf8SpanFormattable
    {
        return ToU8String(value, default, provider);
    }

    /// <summary>
    /// Converts the <see paramref="value"/> to a <see cref="U8String"/> using the specified format and provider.
    /// </summary>
    /// <typeparam name="T">The type of the value to convert.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="provider">The format provider to use.</param>
    /// <returns>U8String representation of the value.</returns>
    // [MethodImpl(MethodImplOptions.AggressiveInlining)] // We might this need after all...
    public static U8String ToU8String<T>(
        this T value,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        if (value is not U8String u8str)
        {
            var length = U8Constants.GetFormattedLength(value);
            return FormatPresized(format, value, provider, length, out var result)
                ? result
                : FormatUnsized(format, value, provider);
        }

        return u8str;
    }

    // TODO:
    // - Really, this should have been moved to U8Conversions or U8String long ago
    // - Use inline-array based or sequence-like builder when FormatExact can fail or
    // when calling into FormatUnsized
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool FormatPresized<T>(
        ReadOnlySpan<char> format,
        T value,
        IFormatProvider? provider,
        int length,
        out U8String result) where T : IUtf8SpanFormattable
    {
        result = default;
        var buffer = new byte[length];
        var success = value.TryFormat(buffer, out length, format, provider);
        if (success)
        {
            result = new(buffer, 0, length);
        }

        return success;
    }

    static U8String FormatUnsized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider)
            where T : IUtf8SpanFormattable
    {
        // TODO: Additional length-resolving heuristics or a stack-allocated into arraypool buffer
        int length;
        var buffer = new byte[64];
        while (!value.TryFormat(buffer, out length, format, provider))
        {
            buffer = new byte[buffer.Length * 2];
        }

        return new(buffer, 0, length);
    }
}
