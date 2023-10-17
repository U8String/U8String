using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

using U8Primitives.InteropServices;
using U8Primitives.IO;

namespace U8Primitives;

// Policy:
// - Constructors must drop the reference if the length is 0.
// - Constructors must not initialize or assign byte[] reference if the length is 0.
// - Constructors must null-terminate the string on allocation of the underlying buffer.
public readonly partial struct U8String
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified UTF-8 bytes.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the length is greater than 0.
    /// </remarks>
    public U8String(ReadOnlySpan<byte> value)
    {
        // Contract:
        // byte[] Value *must* remain null if the length is 0.
        if (value.Length > 0)
        {
            Validate(value);

            var nullTerminate = value[^1] != 0;
            var bytes = new byte[value.Length + (nullTerminate ? 1 : 0)];

            value.CopyToUnsafe(ref bytes.AsRef());

            _value = bytes;
            _inner = new U8Range(0, value.Length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="ImmutableArray{T}"/> of <see cref="byte"/>s.
    /// </summary>
    /// <param name="value">The <see cref="ImmutableArray{T}"/> of <see cref="byte"/>s to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by taking the underlying reference from the <paramref name="value"/> without copying if the length is greater than 0.
    /// </remarks>
    public U8String(ImmutableArray<byte> value)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(value);
        if (bytes?.Length > 0)
        {
            Validate(bytes);
            _value = bytes;
            _inner = new U8Range(0, bytes.Length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="ReadOnlySpan{T}"/> of UTF-8 <see cref="char"/>s.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="char"/>s as UTF-8.
    /// </remarks>
    public U8String(ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            var nullTerminate = value[^1] != 0;
            var length = Encoding.UTF8.GetByteCount(value);
            var bytes = new byte[length + (nullTerminate ? 1 : 0)];
            Encoding.UTF8.GetBytes(value, bytes);

            _value = bytes;
            _inner = new U8Range(0, length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="string"/> as UTF-8.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String(string? value)
    {
        if (value is { Length: > 0 })
        {
            if (U8Interning.TryGetEncoded(value, out this))
            {
                return;
            }

            var nullTerminate = value[^1] != 0;
            var length = Encoding.UTF8.GetByteCount(value);
            var bytes = new byte[length + (nullTerminate ? 1 : 0)];
            Encoding.UTF8.GetBytes(value, bytes);

            _value = bytes;
            _inner = new U8Range(0, length);
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
        if (length > 0) _value = value;
        _inner = new U8Range(offset, length);

        Debug.Assert(Offset >= 0);
        Debug.Assert(_value is null ? Length is 0 : (uint)Length > 0);
        Debug.Assert(value is null || (uint)(offset + length) <= (uint)value.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(byte[]? value, U8Range inner)
    {
        if (inner.Length > 0) _value = value;
        _inner = inner;

        Debug.Assert(Offset >= 0);
        Debug.Assert(_value is null ? Length is 0 : (uint)Length > 0);
        Debug.Assert(value is null || (uint)(
            inner.Offset + inner.Length) <= (uint)value.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        if (value.Length > 0)
        {
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[value.Length + (nullTerminate ? 1 : 0)];

            value.CopyToUnsafe(ref bytes.AsRef());

            _value = bytes;
            _inner = new U8Range(0, value.Length);
        }

        Debug.Assert(Offset >= 0);
        Debug.Assert(_value is null ? Length is 0 : (uint)Length > 0);
    }

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    // Tracks https://github.com/dotnet/runtime/issues/87569
    public static U8String Create(/*params*/ ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ImmutableArray{byte})"/>
    public static U8String Create(ImmutableArray<byte> value) => new(value);

    /// <inheritdoc cref="U8String(string)"/>
    public static U8String Create(string value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    public static U8String Create(/*params*/ ReadOnlySpan<char> value) => new(value);

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T)"/>
    public static U8String Create<T>(T value)
        where T : IUtf8SpanFormattable
    {
        // Specialized path that uses default format and null provider
        // which improves codegen and allows us to use literals.
        if (value is not U8String u8str)
        {
            if (typeof(T).IsValueType && TryFormatLiteral(value, out var result))
            {
                return result;
            }

            if (TryFormatPresized(value, out result))
            {
                return result;
            }

            return FormatUnsized(value);
        }

        return u8str;
    }

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T, ReadOnlySpan{char})"/>
    public static U8String Create<T>(T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
        return Create(value, format, null);
    }

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T, IFormatProvider?)"/>
    public static U8String Create<T>(T value, IFormatProvider? provider)
        where T : IUtf8SpanFormattable
    {
        return Create(value, default, provider);
    }

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T, ReadOnlySpan{char}, IFormatProvider?)"/>
    public static U8String Create<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider)
        where T : IUtf8SpanFormattable
    {
        if (value is not U8String u8str)
        {
            if (TryFormatPresized(value, format, provider, out var result))
            {
                return result;
            }

            return FormatUnsized(value, format, provider);
        }

        return u8str;
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="value"/> without verifying
    /// if it is a valid UTF-8 sequence.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the length is greater than 0.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String CreateUnchecked(ReadOnlySpan<byte> value)
    {
        return new(value, skipValidation: true);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="value"/> without verifying
    /// if it is a valid UTF-8 sequence.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// <para>
    /// The <see cref="U8String"/> will be created by taking the underlying reference from the
    /// <paramref name="value"/> without copying if the length is greater than 0.
    /// </para>
    /// <para>
    /// This is a safe variant of <see cref="U8Marshal.Create(byte[])"/> which does not allocate.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String CreateUnchecked(ImmutableArray<byte> value)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(value);
        if (bytes != null)
        {
            return new(bytes, 0, bytes.Length);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Read(SafeFileHandle handle, long offset = 0)
    {
        return handle.ReadToU8String(offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<U8String> ReadAsync(
        SafeFileHandle handle, long offset = 0, CancellationToken ct = default)
    {
        return handle.ReadToU8StringAsync(offset, ct);
    }

    public static bool TryCreate(ReadOnlySpan<byte> value, out U8String result)
    {
        if (IsValid(value))
        {
            result = new(value, skipValidation: true);
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryCreate(ImmutableArray<byte> value, out U8String result)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(value);

        if (IsValid(bytes))
        {
            result = new(bytes, 0, bytes?.Length ?? 0);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Clones the <see cref="U8String"/> by copying the underlying
    /// <see cref="Length"/> of bytes into a new <see cref="U8String"/> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is useful when a particular <see cref="U8String"/> is a slice of a larger
    /// <see cref="U8String"/> that is no longer needed and can be garbage collected.
    /// This allows the GC to collect the larger <see cref="U8String"/> and reclaim the memory
    /// it would hold otherwise.
    /// </para>
    /// <para>
    /// Example:
    /// <code>
    /// var articleText = await httpClient.GetU8StringAsync("https://example.com/article.txt");
    /// var previewText = articleText[..100].Clone();
    /// </code>
    /// </para>
    /// </remarks>
    public U8String Clone() => new(this, skipValidation: true);

    /// <inheritdoc />
    object ICloneable.Clone() => Clone();
}
