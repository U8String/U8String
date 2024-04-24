using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

using Microsoft.Win32.SafeHandles;

using U8.InteropServices;
using U8.IO;
using U8.Primitives;
using U8.Shared;

namespace U8;

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the length is greater than 0.
    /// </remarks>
    public U8String(byte[] value)
    {
        // TODO: move newarr and memmove into a local function and forceinline
        // null, length and validity checks?
        ThrowHelpers.CheckNull(value);

        var span = (ReadOnlySpan<byte>)value;
        if (span.Length > 0)
        {
            Validate(span);
            var nullTerminate = span[^1] != 0;
            var bytes = new byte[(nint)(uint)span.Length + (nullTerminate ? 1 : 0)];

            span.CopyToUnsafe(ref bytes.AsRef());

            _value = bytes;
        }

        _inner = new U8Range(0, span.Length);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified UTF-8 bytes.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the length is greater than 0.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String(ReadOnlySpan<byte> value)
    {
        // AggressiveInlining is here to allow the compiler to unroll
        // memmove and optimize away length and null-termination checks.
        if (value.Length > 0)
        {
            Validate(value);

            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)value.Length + (nullTerminate ? 1 : 0)];

            value.CopyToUnsafe(ref bytes.AsRef());

            _value = bytes;
        }

        _inner = new U8Range(0, value.Length);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="ImmutableArray{T}"/> of <see cref="byte"/>s.
    /// </summary>
    /// <param name="value">The <see cref="ImmutableArray{T}"/> of <see cref="byte"/>s to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-8 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by taking the underlying reference from the <paramref name="value"/> without copying the bytes if the length is greater than 0.
    /// </remarks>
    public U8String(ImmutableArray<byte> value)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(value);
        if (bytes != null)
        {
            var span = bytes.AsSpan();
            if (span.Length > 0)
            {
                Validate(span);
                _value = bytes;
                _inner = new U8Range(0, span.Length);
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-16 data.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="string"/> as UTF-8.
    /// </remarks>
    public U8String(string value)
    {
        ThrowHelpers.CheckNull(value);

        if (value.Length > 0)
        {
            var length = Encoding.UTF8.GetByteCount(value);
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)length + (nullTerminate ? 1 : 0)];

            var result = Utf8.FromUtf16(
                source: value,
                destination: bytes,
                charsRead: out _,
                bytesWritten: out length,
                replaceInvalidSequences: false,
                isFinalBlock: true);

            if (result != OperationStatus.Done)
            {
                ThrowHelpers.InvalidUtf8();
            }

            _value = bytes;
            _inner = new U8Range(0, length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s to create the <see cref="U8String"/> from.</param>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> contains malformed UTF-16 data.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by encoding the <see cref="char"/>s as UTF-8.
    /// </remarks>
    public U8String(ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            var length = Encoding.UTF8.GetByteCount(value);
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)length + (nullTerminate ? 1 : 0)];

            var result = Utf8.FromUtf16(
                source: value,
                destination: bytes,
                charsRead: out _,
                bytesWritten: out length,
                replaceInvalidSequences: false,
                isFinalBlock: true);

            if (result != OperationStatus.Done)
            {
                ThrowHelpers.InvalidUtf8();
            }

            _value = bytes;
            _inner = new U8Range(0, length);
        }
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified string interpolation.
    /// </summary>
    /// <param name="handler">The string interpolation handler.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by writing the contents of the interpolated handler
    /// constructed by Roslyn into a newly allocated buffer.
    /// <para/>
    /// This method has consuming semantics and calls <see cref="InlineU8Builder.Dispose"/>
    /// on the provided <paramref name="handler"/> after the <see cref="U8String"/> is created.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String(ref InlineU8Builder handler)
    {
        this = new U8String(handler.Written, skipValidation: true);
        handler.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified string interpolation with the specified format provider.
    /// </summary>
    /// <param name="provider">The format provider to use.</param>
    /// <param name="handler">The string interpolation handler.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by writing the contents of the interpolated handler
    /// constructed by Roslyn into a newly allocated buffer.
    /// <para/>
    /// This method has consuming semantics and calls <see cref="InlineU8Builder.Dispose"/>
    /// on the provided <paramref name="handler"/> after the <see cref="U8String"/> is created.
    /// </remarks>
#pragma warning disable IDE0060, RCS1163 // Unused parameter. Why: it is passed to the handler ctor.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String(IFormatProvider provider,
        [InterpolatedStringHandlerArgument(nameof(provider))] ref InlineU8Builder handler)
    {
        this = new U8String(handler.Written, skipValidation: true);
        handler.Dispose();
    }
#pragma warning restore IDE0060, RCS1163

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified null-terminated UTF-8 string
    /// represented by a pointer to its first byte.
    /// </summary>
    /// <param name="str">A pointer to the first byte of a null-terminated UTF-8 string.</param>
    /// <exception cref="NullReferenceException">Thrown when <paramref name="str"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="str"/> contains malformed UTF-8 data.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the resulting <see cref="U8String"/> would exceed the maximum supported length.
    /// </exception>
    /// <exception cref="AccessViolationException">
    /// Thrown when <paramref name="str"/> is not a valid pointer or when the null-terminator is not found within readable memory.
    /// This exception cannot be caught.
    /// </exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the bytes starting at the <paramref name="str"/>
    /// up to the first null byte if the null byte offset is greater than 0.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe U8String(byte* str)
    {
        // TODO: This pretty much traverses the source three times:
        // 1. Finds NUL
        // 2. Validates UTF-8
        // 3. Copies to newly allocated byte[]
        // Ideally, at least the first two steps need to be combined,
        // to get more work done per CPU cycle as this otherwise will be
        // quite a bit slower than creating from sources of known length.
        this = new(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(str));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        // Quite a bit of code relies on this constructor to produce the exact
        // value of bytes, often unconditionally consuming the resulting
        // newvalue._value and newvalue._value.Length - 1.
        // You *MUST* make sure such callsites are updated should this constructor
        // change its behavior.
        if (value.Length > 0)
        {
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)value.Length + (nullTerminate ? 1 : 0)];

            value.CopyToUnsafe(ref bytes.AsRef());

            _value = bytes;
        }

        _inner = new U8Range(0, value.Length);

        Debug.Assert(Offset >= 0);
        Debug.Assert(_value is null ? Length is 0 : (uint)Length > 0);
    }

    /// <summary>
    /// Direct constructor of <see cref="U8String"/> from a <see cref="byte"/> array.
    /// </summary>
    /// <remarks>
    /// Contract:
    /// The constructor will *always* drop the reference if the length is 0.
    /// Consequently, the value *must* remain null if the length is 0.
    /// </remarks>
    [DebuggerStepThrough]
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
    internal U8String(byte[] value, int length, bool neverEmpty)
    {
        _value = value;
        _inner = new U8Range(0, length);

        Debug.Assert(neverEmpty);
        Debug.Assert(value != null);
        Debug.Assert(length > 0 && length <= value.Length);
    }

    /// <inheritdoc cref="U8String(byte[])"/>
    public static U8String Create(byte[] value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{byte})"/>
    public static U8String Create(ReadOnlySpan<byte> value) => new(value);

    /// <inheritdoc cref="U8String(ImmutableArray{byte})"/>
    public static U8String Create(ImmutableArray<byte> value) => new(value);

    /// <inheritdoc cref="U8String(string)"/>
    public static U8String Create(string value) => new(value);

    /// <inheritdoc cref="U8String(ReadOnlySpan{char})"/>
    public static U8String Create(ReadOnlySpan<char> value) => new(value);

    /// <inheritdoc cref="U8String(byte*)"/>
    public static unsafe U8String Create(byte* ptr) => new(ptr);

    /// <summary>
    /// Converts the <see cref="bool"/> value to its equivalent UTF-8 string representation (either "True" or "False").
    /// </summary>
    public static U8String Create(bool value) => value ? u8("True") : u8("False");

    /// <summary>
    /// Converts the <see cref="byte"/> value to its equivalent UTF-8 string representation.
    /// </summary>
    public static U8String Create(byte value) => U8Literals.GetByte(value);

    public static U8String Create(char value)
    {
        if (value <= 0x7F)
        {
            Debug.Assert(U8Literals.Runes.IsInRange(value));
            return U8Literals.Runes.GetValueUnchecked(value);
        }

        var codepoint = (ReadOnlySpan<byte>)(
            value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
        var bytes = new byte[codepoint.Length + 1];

        ref var dst = ref bytes.AsRef();
        if (codepoint.Length is 2)
        {
            dst.Cast<byte, ushort>() = codepoint.AsRef().Cast<byte, ushort>();
        }
        else
        {
            dst.Cast<byte, ushort>() = codepoint.AsRef().Cast<byte, ushort>();
            dst.Add(2) = codepoint.AsRef(2);
        }

        return new(bytes, codepoint.Length, neverEmpty: true);
    }

    public static U8String Create(Rune value)
    {
        if (value.Value <= 0x7F)
        {
            Debug.Assert(U8Literals.Runes.IsInRange(value.Value));
            return U8Literals.Runes.GetValueUnchecked(value.Value);
        }

        var codepoint = (ReadOnlySpan<byte>)(value.Value switch
        {
            <= 0x7FF => value.AsTwoBytes(),
            <= 0xFFFF => value.AsThreeBytes(),
            _ => value.AsFourBytes()
        });
        var bytes = new byte[codepoint.Length + 1];

        ref var dst = ref bytes.AsRef();
        if (codepoint.Length is 2)
        {
            dst.Cast<byte, ushort>() = codepoint.AsRef().Cast<byte, ushort>();
        }
        else if (codepoint.Length is 3)
        {
            dst.Cast<byte, ushort>() = codepoint.AsRef().Cast<byte, ushort>();
            dst.Add(2) = codepoint.AsRef(2);
        }
        else
        {
            dst.Cast<byte, uint>() = codepoint.AsRef().Cast<byte, uint>();
        }

        return new(bytes, codepoint.Length, neverEmpty: true);
    }

    /// <inheritdoc cref="U8StringExtensions.ToU8String{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Create<T>(T value)
        where T : IUtf8SpanFormattable
    {
        // Specialized path that uses default format and null provider
        // which improves codegen and allows us to use literals.
        if (value is not U8String u8str)
        {
            // This is written in the exact way which prevents the cached
            // literal from spilling to the stack. Codegen *must* be validated
            // whenever this method is changed.
            if (typeof(T) == typeof(byte))
            {
                return U8Literals.GetByte((byte)(object)value);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                if (!U8Literals.Numbers.IsInRange((sbyte)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((sbyte)(object)value);
            }
            else if (typeof(T) == typeof(short))
            {
                if (!U8Literals.Numbers.IsInRange((short)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((short)(object)value);
            }
            else if (typeof(T) == typeof(ushort))
            {
                if (!U8Literals.Numbers.IsInRange((ushort)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((ushort)(object)value);
            }
            else if (typeof(T) == typeof(int))
            {
                if (!U8Literals.Numbers.IsInRange((int)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((int)(object)value);
            }
            else if (typeof(T) == typeof(uint))
            {
                if (!U8Literals.Numbers.IsInRange((nint)(uint)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((nint)(uint)(object)value);
            }
            else if (typeof(T) == typeof(long))
            {
                if (!U8Literals.Numbers.IsInRange((nint)(long)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((nint)(long)(object)value);
            }
            else if (typeof(T) == typeof(ulong))
            {
                if (!U8Literals.Numbers.IsInRange((nint)(ulong)(object)value))
                    goto FormatNew;
                return U8Literals.Numbers.GetValueUnchecked((nint)(ulong)(object)value);
            }
            else if (typeof(T) == typeof(char))
            {
                return Create((char)(object)value);
            }
            else if (typeof(T) == typeof(Rune))
            {
                return Create((Rune)(object)value);
            }

        FormatNew:
            return CreateCore(value);

            static U8String CreateCore(T value)
            {
                if (TryFormatPresized(value, out var result))
                {
                    return result;
                }

                return FormatUnsized(value);
            }
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
            provider ??= CultureInfo.InvariantCulture;
            if (TryFormatPresized(value, format, provider, out var result))
            {
                return result;
            }

            return FormatUnsized(value, format, provider);
        }

        return u8str;
    }

    /// <inheritdoc cref="U8String(ref InlineU8Builder)"/>
    public static U8String Create(ref InlineU8Builder handler)
    {
        return new(ref handler);
    }

    /// <inheritdoc cref="U8String(IFormatProvider, ref InlineU8Builder)"/>
    public static U8String Create(IFormatProvider provider,
        [InterpolatedStringHandlerArgument(nameof(provider))] ref InlineU8Builder handler)
    {
        return new(provider, ref handler);
    }

    // TODO: Documentation
    // TODO: byte[] counterpart
    public static U8String CreateLossy(string value)
    {
        ThrowHelpers.CheckNull(value);

        return CreateLossy(value.AsSpan());
    }

    public static U8String CreateLossy(ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            var length = Encoding.UTF8.GetByteCount(value);
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)length + (nullTerminate ? 1 : 0)];

            var result = Utf8.FromUtf16(
                source: value,
                destination: bytes,
                charsRead: out _,
                bytesWritten: out length,
                replaceInvalidSequences: true,
                isFinalBlock: true);

            Debug.Assert(result is OperationStatus.Done);

            return new(bytes, 0, length);
        }

        return default;
    }

    /// <inheritdoc cref="FromAscii(ReadOnlySpan{char})"/>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String FromAscii(string value)
    {
        ThrowHelpers.CheckNull(value);
        return FromAscii(value.AsSpan());
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from the specified <paramref name="value"/>
    /// containing ASCII characters.
    /// </summary>
    /// <exception cref="FormatException">Thrown when the <paramref name="value"/> is not valid ASCII sequence.</exception>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by performing a narrowing copy of the <see cref="char"/>s
    /// from the <paramref name="value"/> if the length is greater than 0. Unlike the regular constructor,
    /// this method does not have to calculate the UTF-8 byte count of the <paramref name="value"/>
    /// and traverses it only once.
    /// </remarks>
    public static U8String FromAscii(ReadOnlySpan<char> value)
    {
        if (value.Length > 0)
        {
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)value.Length + (nullTerminate ? 1 : 0)];

            var status = Ascii.FromUtf16(
                source: value,
                destination: bytes,
                bytesWritten: out _);

            if (status != OperationStatus.Done)
            {
                ThrowHelpers.InvalidAscii();
            }

            return new(bytes, 0, value.Length);
        }

        return default;
    }

    public static bool TryCreate(byte[]? value, out U8String result)
    {
        if (value != null)
        {
            var span = (ReadOnlySpan<byte>)value;
            if (IsValid(span))
            {
                result = new(span, skipValidation: true);
                return true;
            }
        }

        result = default;
        return false;
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

    public static bool TryCreate(string? value, out U8String result)
    {
        if (value != null)
        {
            return TryCreate(value.AsSpan(), out result);
        }

        result = default;
        return false;
    }

    public static bool TryCreate(ReadOnlySpan<char> value, out U8String result)
    {
        var success = true;

        if (value.Length > 0)
        {
            var length = Encoding.UTF8.GetByteCount(value);
            var nullTerminate = value[^1] != 0;
            var bytes = new byte[(nint)(uint)length + (nullTerminate ? 1 : 0)];

            var status = Utf8.FromUtf16(
                source: value,
                destination: bytes,
                charsRead: out _,
                bytesWritten: out length,
                replaceInvalidSequences: false,
                isFinalBlock: true);

            if (status == OperationStatus.Done)
            {
                result = new(bytes, 0, length);
                return true;
            }

            success = false;
        }

        result = default;
        return success;
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
