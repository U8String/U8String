using System.Collections.Immutable;
using System.Text;

namespace U8.Extensions;

/// <summary>
/// Provides a set of static methods for shorthand <see cref="U8String"/> construction syntax.
/// </summary>
/// <remarks>
/// This class is intended to be imported with <c>global using static U8.Extensions.Syntax</c>.
/// </remarks>
public static unsafe class Syntax
{
    // TODO: Make this STR(...) to inflict psychic damage on Java developers?
    /// <inheritdoc cref="U8String(byte[])"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(byte[] value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ImmutableArray{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(ImmutableArray<byte> value) => new(value);

    /// <inheritdoc cref="U8String(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(string value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(ReadOnlySpan<char> value) => new(value);

    /// <inheritdoc cref="U8String(byte*)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(byte* value) => new(value);

    /// <inheritdoc cref="U8String.Create(bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(bool value) => U8String.Create(value);

    /// <inheritdoc cref="U8String.Create(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(byte value) => U8String.Create(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(char value) => U8String.Create(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(Rune value) => U8String.Create(value);

    /// <inheritdoc cref="U8String.Create{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8<T>(T value)
        where T : IUtf8SpanFormattable => U8String.Create(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8<T>(T value, string format)
        where T : IUtf8SpanFormattable => U8String.Create(value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8<T>(T value, IFormatProvider? provider)
        where T : IUtf8SpanFormattable => U8String.Create(value, provider);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8<T>(T value, string format, IFormatProvider? provider)
        where T : IUtf8SpanFormattable => U8String.Create(value, format, provider);

    /// <inheritdoc cref="U8String(ref InlineU8Builder)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String u8(ref InlineU8Builder handler) => new(ref handler);
}