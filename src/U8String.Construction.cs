using System.Diagnostics;

namespace U8Primitives;

public readonly partial struct U8String
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified UTF-8 bytes.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> contains invalid UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/>.
    /// </remarks>
    public U8String(ReadOnlySpan<byte> value)
    {
        if (!value.IsEmpty)
        {
            Validate(value);
            Value = value.ToArray();
            Inner = new InnerOffsets(0, value.Length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="char"/>s as UTF-8.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String(ReadOnlySpan<char> value)
    {
        if (!value.IsEmpty)
        {
            this = Parse(value, null);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="string"/> as UTF-8.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this = Parse(value.AsSpan(), null);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(byte[]? value, int offset, int length)
    {
        Value = value;
        Inner = new InnerOffsets(offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        Value = value.ToArray();
        Inner = new InnerOffsets(0, value.Length);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified UTF-8 bytes.
    /// </summary>
    /// <param name="items">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="items"/> contains invalid UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="items"/>.
    /// </remarks>
    // Tracks https://github.com/dotnet/runtime/issues/87569
    public static U8String Create(/*params*/ ReadOnlySpan<byte> items) => new(items);

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T, IFormatProvider?)"/>
    public static U8String Create<T>(T value, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        return value.ToU8String(provider);
    }

    /// <inheritdoc />
    public object Clone() => new U8String(AsSpan(), skipValidation: true);
}
