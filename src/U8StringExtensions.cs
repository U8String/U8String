using System.Diagnostics;

using U8Primitives;

namespace System;

#pragma warning disable CA1305 // Specify IFormatProvider
/// <summary>
/// Provides extension methods to integrate <see cref="U8String"/> with the .NET type system.
/// </summary>
public static class U8StringExtensions
{
    /// <inheritdoc cref="U8String(string?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this string? value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this Span<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this Span<char> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String(this ReadOnlySpan<char> value) => new(value);

    /// <summary>
    /// A short-circuit overload against unintended <see cref="ToU8String{T}(T,IFormatProvider?)"/> on a <see cref="U8String"/>.
    /// </summary>
    public static U8String ToU8String(this U8String value) => value;

    /// <summary>
    /// Converts the value to a U8String using the default format and a provider, if specified.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="provider">The format provider to use.</param>
    /// <returns>U8String representation of the value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ToU8String<T>(this T value, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        return ToU8String(value, default, provider);
    }

    /// <summary>
    /// Converts the value to a U8String using the specified format and provider.
    /// </summary>
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
            var length = U8Constants.GetFormattedLength<T>();
            return length != 0
                ? FormatExact(format, value, provider, length)
                : FormatUnsized(format, value, provider);
        }

        return u8str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static U8String FormatExact<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider, int length)
            where T : IUtf8SpanFormattable
    {
        var buffer = new byte[length];
        var result = value.TryFormat(buffer, out length, format, provider);

        Debug.Assert(result);
        return new U8String(buffer, 0, length);
    }

    static U8String FormatUnsized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider)
            where T : IUtf8SpanFormattable
    {
        var buffer = new byte[32];
    Retry:
        if (value.TryFormat(buffer, out var length, format, provider))
        {
            return new U8String(buffer, 0, length);
        }

        // Limits???? Check what CoreLib does
        buffer = new byte[buffer.Length * 2];
        goto Retry;
    }
}
