using System.Diagnostics;

namespace U8Primitives;

public readonly partial struct U8String
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified UTF-8 bytes.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> contains malformed UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the span is not empty.
    /// </remarks>
    public U8String(ReadOnlySpan<byte> value)
    {
        // Contract:
        // byte[] Value *must* remain null if the length is 0.
        // TODO: Consider null-terminating the string to opportunistically
        // enable zero-copy interop with native code. Or not?
        if (value.Length > 0)
        {
            Validate(value);
            _value = value.ToArray();
            _inner = new InnerOffsets(0, value.Length);
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
        if (value.Length > 0)
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

    /// <summary>
    /// Direct constructor of <see cref="U8String"/> from a <see cref="byte"/> array.
    /// </summary>
    /// <remarks>
    /// Contract:
    /// The constructor will *always* drop the reference if the length is 0.
    /// Consequently, the value *must* remain null if the length is 0.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(byte[]? value, int offset, int length)
    {
        Debug.Assert((value?.Length ?? 0) >= offset + length);

        // TODO: Deduplicate the length check from the callers.
        if (length > 0)
        {
            _value = value;
            _inner = new InnerOffsets(offset, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        if (value.Length > 0)
        {
            _value = value.ToArray();
            _inner = new InnerOffsets(0, value.Length);
        }
    }

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    // Tracks https://github.com/dotnet/runtime/issues/87569
    public static U8String Create(/*params*/ ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    public static U8String Create(/*params*/ ReadOnlySpan<char> value) => new(value);

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T)"/>
    public static U8String Create<T>(T value)
        where T : IUtf8SpanFormattable
    {
        // TODO: Invert where the implementation lives?
        return value.ToU8String();
    }

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T, IFormatProvider?)"/>
    public static U8String Create<T>(T value, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        return value.ToU8String(provider);
    }

    /// <inheritdoc />
    public object Clone() => new U8String(this, skipValidation: true);
}
