using System.Buffers;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using U8.Primitives;
using U8.Shared;

namespace U8;

[InterpolatedStringHandler]
[EditorBrowsable(EditorBrowsableState.Advanced)]
#pragma warning disable IDE0038 // Use pattern matching. Why: non-boxing interface resolution on structs.
// TODO: Add padding support
// TODO: Deduplicate core impl.
public /* ref */ struct InlineU8Builder : IInterpolatedHandler
{
    const int InitialRentLength = 512;

    InlineBuffer64 _inline;
    byte[]? _rented;

    public IFormatProvider Provider { get; private set; }

    public int BytesWritten { get; private set; }

    public ReadOnlySpan<byte> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_rented ?? _inline.AsSpan()).SliceUnsafe(0, BytesWritten);
    }

    Span<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_rented ?? _inline.AsSpan()).SliceUnsafe(BytesWritten);
    }


    int IInterpolatedHandler.BytesWritten
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => BytesWritten;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => BytesWritten = value;
    }

    Span<byte> IInterpolatedHandler.Free => Free;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineU8Builder()
    {
        Unsafe.SkipInit(out _inline);
        Provider = CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineU8Builder(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        Unsafe.SkipInit(out _inline);

        if (length > InlineBuffer64.Length)
        {
            _rented = ArrayPool<byte>.Shared.Rent(length);
        }

        Provider = CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InlineU8Builder(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider = null)
    {
        Unsafe.SkipInit(out _inline);

        var initialLength = literalLength + (formattedCount * 12);
        if (initialLength > InlineBuffer64.Length)
        {
            _rented = ArrayPool<byte>.Shared.Rent(initialLength);
        }

        Provider = formatProvider ?? CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral([ConstantExpected] string s) => U8Interpolation.AppendLiteral(ref this, s);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AppendLiteral(ReadOnlySpan<char> s) => U8Interpolation.AppendLiteral(ref this, s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(bool value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(char value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Rune value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(U8String value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(U8String? value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<byte> value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlySpan<char> value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value) => U8Interpolation.AppendFormatted(ref this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable => U8Interpolation.AppendFormatted(ref this, value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendBytes(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytes(ref this, bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendBytesInlined(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytesInlined(ref this, bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendBytesUnchecked(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytesUnchecked(ref this, bytes);

    internal void EnsureInitialized()
    {
        Provider ??= CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EnsureCapacity(int length)
    {
        if (Free.Length < length)
        {
            Grow(length);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void IInterpolatedHandler.Grow()
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _rented;
        var written = (rented ?? _inline.AsSpan())
            .SliceUnsafe(0, BytesWritten);

        var newLength = rented is null
            ? InitialRentLength
            : rented.Length * 2;

        var newArr = arrayPool.Rent(newLength);

        written.CopyToUnsafe(ref newArr.AsRef());
        _rented = newArr;

        if (rented != null)
        {
            arrayPool.Return(rented);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IInterpolatedHandler.Grow(int hint) => Grow(hint);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Grow(int hint)
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _rented;
        var written = (rented ?? _inline.AsSpan())
            .SliceUnsafe(0, BytesWritten);

        var newLength = rented is null
            ? InitialRentLength
            : rented.Length * 2;

        newLength = Math.Max(newLength, written.Length + hint);

        var newArr = arrayPool.Rent(newLength);

        written.CopyToUnsafe(ref newArr.AsRef());
        _rented = newArr;

        if (rented != null)
        {
            arrayPool.Return(rented);
        }
    }

    /// <summary>
    /// Resets the buffer and returns pooled array if applicable.
    /// </summary>
    /// <remarks>
    /// The buffer may be reused after calling this method.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        BytesWritten = 0;
        (var rented, _rented) = (_rented, null);

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    internal void ArrayPoolSafeDispose()
    {
        BytesWritten = 0;
        var rented = Interlocked.Exchange(ref _rented, null);
        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}

[InterpolatedStringHandler]
[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable IDE0038, RCS1220 // Use pattern matching. Why: non-boxing interface resolution on structs.
public struct PooledU8Builder
{
    const int InitialRentLength = 512;

    readonly IFormatProvider? _provider;
    byte[] _array;

    public int BytesWritten { get; private set; }

    public readonly ReadOnlySpan<byte> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array.SliceUnsafe(0, BytesWritten);
    }

    internal readonly ReadOnlyMemory<byte> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array.AsMemory(0, BytesWritten);
    }

    readonly Span<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _array.SliceUnsafe(BytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledU8Builder()
    {
        _array = ArrayPool<byte>.Shared.Rent(InitialRentLength);
        _provider = CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledU8Builder(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        _array = ArrayPool<byte>.Shared.Rent(length);
        _provider = CultureInfo.InvariantCulture;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PooledU8Builder(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider = null)
    {
        _array = ArrayPool<byte>.Shared.Rent(literalLength + (formattedCount * 96));
        _provider = formatProvider ?? CultureInfo.InvariantCulture;
    }

    // Reference: https://github.com/dotnet/runtime/issues/93501
    // Refactor once inlined TryGetBytes gains UTF8EncodingSealed.ReadUtf8 call
    // which JIT/AOT can optimize away for string literals, eliding the transcoding.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral([ConstantExpected] string s)
    {
        if (s.Length > 0)
        {
            if (s.Length is 1 && char.IsAscii(s[0]))
            {
                AppendByte((byte)s[0]);
            }
            else if (s.Length is 2
                && char.IsAscii(s[0])
                && char.IsAscii(s[1]))
            {
                AppendTwoBytes((ushort)(s[0] | ((uint)s[1] << 8)));
            }
            else
            {
                AppendConstantString(s);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AppendLiteral(ReadOnlySpan<char> s)
    {
    Retry:
        if (Encoding.UTF8.TryGetBytes(s, Free, out var written))
        {
            BytesWritten += written;
            return;
        }

        // We can't use the length * 2 or * 3 hint here because
        // it will fail interpolation for 1-1.5GiB strings which
        // is otherwise a legal operation.
        Grow();
        goto Retry;
    }

    public void AppendFormatted(bool value)
    {
        AppendBytes(value ? "True"u8 : "False"u8);
    }

    public void AppendFormatted(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        if (char.IsAscii(value))
        {
            AppendByte((byte)value);
            return;
        }

        AppendBytes(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
    }

    public void AppendFormatted(Rune value)
    {
        if (value.IsAscii)
        {
            AppendByte((byte)value.Value);
            return;
        }

        AppendBytes(value.Value switch
        {
            <= 0x7FF => value.AsTwoBytes(),
            <= 0xFFFF => value.AsThreeBytes(),
            _ => value.AsFourBytes()
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(U8String value)
    {
        if (!value.IsEmpty)
        {
            AppendBytes(value.UnsafeSpan);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(U8String? value)
    {
        if (value is { IsEmpty: false } text)
        {
            AppendBytes(text.UnsafeSpan);
        }
    }

    public void AppendFormatted(ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        AppendBytes(value);
    }

    public void AppendFormatted(string? value)
    {
        if (value is not null)
        {
            AppendLiteral(value.AsSpan());
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        AppendLiteral(value);
    }

    public void AppendFormatted<T>(T value)
    {
    Retry:
        if (typeof(T) == typeof(U8String))
        {
            AppendFormatted((U8String)(object)value!);
            return;
        }
        else if (typeof(T) == typeof(U8Builder))
        {
            AppendBytes(((U8Builder)(object)value!).Written);
            return;
        }
        else if (typeof(T) == typeof(U8String?))
        {
            AppendFormatted((U8String?)(object)value!);
            return;
        }
        else if (value is IUtf8SpanFormattable)
        {
            if (((IUtf8SpanFormattable)value)
                .TryFormat(Free, out var written, default, _provider))
            {
                BytesWritten += written;
                return;
            }
        }
        else if (typeof(T).IsEnum)
        {
#nullable disable
            var formattable = new U8EnumFormattable<T>(value);
#nullable restore
            if (formattable.TryFormat(Free, out var written))
            {
                BytesWritten += written;
                return;
            }
        }
        else if (typeof(T) == typeof(ImmutableArray<byte>))
        {
            AppendFormatted(((ImmutableArray<byte>)(object)value!).AsSpan());
            return;
        }
        else if (typeof(T) == typeof(byte[]))
        {
            AppendFormatted(Unsafe.As<byte[]?>(value).AsSpan());
            return;
        }
        else if (typeof(T) == typeof(string))
        {
            AppendFormatted(Unsafe.As<string?>(value));
            return;
        }
        else
        {
            UnsupportedAppend<T>();
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AppendFormatted<T>(T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
    Retry:
        if (value.TryFormat(Free, out var written, format, _provider))
        {
            BytesWritten += written;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void AppendConstantString([ConstantExpected] string s)
    {
        var literal = U8Literals.Utf16.GetLiteral(s);
        AppendBytes(literal.SliceUnsafe(0, literal.Length - 1));
    }

    // This may seem surprising but we can't waste precious inlining budget
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void AppendByte(byte value)
    {
    Retry:
        var free = Free;
        if (free.Length > 0)
        {
            free[0] = value;
            BytesWritten++;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void AppendTwoBytes(ushort b01)
    {
    Retry:
        var free = Free;
        if (free.Length > 1)
        {
            free.AsRef().Cast<byte, ushort>() = b01;
            BytesWritten += 2;
            return;
        }

        Grow();
        goto Retry;
    }

    internal void AppendBytes(ReadOnlySpan<byte> bytes)
    {
    Retry:
        var free = Free;
        if (free.Length >= bytes.Length)
        {
            bytes.CopyToUnsafe(ref free.AsRef());
            BytesWritten += bytes.Length;
            return;
        }

        Grow(bytes.Length);
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendBytesInlined(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0) return;
        Retry:
        var free = Free;
        if (free.Length >= bytes.Length)
        {
            if (bytes.Length is 1)
            {
                free[0] = bytes[0];
            }
            else
            {
                bytes.CopyToUnsafe(ref free.AsRef());
            }

            BytesWritten += bytes.Length;
            return;
        }

        Grow(bytes.Length);
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AppendBytesUnchecked(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(Free.Length >= bytes.Length);

        bytes.CopyToUnsafe(ref Free.AsRef());
        BytesWritten += bytes.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Grow()
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _array;
        var newArr = arrayPool.Rent(_array.Length * 2);

        rented
            .SliceUnsafe(0, BytesWritten)
            .CopyToUnsafe(ref newArr.AsRef());

        _array = newArr;
        arrayPool.Return(rented);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Grow(int hint)
    {
        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _array;
        var bytesWritten = BytesWritten;
        var length = Math.Max(rented.Length * 2, bytesWritten + hint);
        var newArr = arrayPool.Rent(length);

        rented
            .SliceUnsafe(0, bytesWritten)
            .CopyToUnsafe(ref newArr.AsRef());

        _array = newArr;
        arrayPool.Return(rented);
    }

    /// <summary>
    /// Resets the buffer and returns pooled array if applicable.
    /// </summary>
    /// <remarks>
    /// The buffer may be reused after calling this method.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        BytesWritten = 0;
        (var rented, _array) = (_array, null!);

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    [DoesNotReturn, StackTraceHidden]
    static void UnsupportedAppend<T>()
    {
        throw new NotSupportedException(
            $"\nCannot append a value of type '{typeof(T)}' which does not implement '{typeof(IUtf8SpanFormattable)}' or is not '{typeof(Enum)}' or '{typeof(byte[])}'.");
    }
}

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryFormatPresized<T>(T value, out U8String result)
        where T : IUtf8SpanFormattable
    {
        var length = GetFormattedLength<T>();
        var buffer = new byte[length];
        var success = value.TryFormat(buffer, out length, default, CultureInfo.InvariantCulture);

        result = new(buffer, 0, length);
        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryFormatPresized<T>(
        T value, ReadOnlySpan<char> format, IFormatProvider provider, out U8String result)
            where T : IUtf8SpanFormattable
    {
        var length = GetFormattedLength<T>();
        var buffer = new byte[length]; // Most cases will be shorter than this, no need to add extra null terminator
        var success = value.TryFormat(buffer, out length, format, provider);

        result = new(buffer, 0, length);
        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetFormattedLength<T>() where T : IUtf8SpanFormattable
    {
        // This table priotizes the common case formatted length
        // given invariant culture and no format string. We pay extra
        // cost for e.g. long dates to ensure minimal heap footprint for
        // numerous u8(DateTime.UtcNow) and similar.
        if (typeof(T) == typeof(sbyte)) return 8;
        if (typeof(T) == typeof(char)) return 4;
        if (typeof(T) == typeof(Rune)) return 8;
        if (typeof(T) == typeof(short)) return 8;
        if (typeof(T) == typeof(ushort)) return 8;
        if (typeof(T) == typeof(int)) return 16;
        if (typeof(T) == typeof(uint)) return 16;
        if (typeof(T) == typeof(long)) return 24;
        if (typeof(T) == typeof(ulong)) return 24;
        if (typeof(T) == typeof(nint)) return 24;
        if (typeof(T) == typeof(nuint)) return 24;
        if (typeof(T) == typeof(float)) return 16;
        if (typeof(T) == typeof(double)) return 24;
        if (typeof(T) == typeof(decimal)) return 32;
        if (typeof(T) == typeof(DateTime)) return 24;
        if (typeof(T) == typeof(DateTimeOffset)) return 32;
        if (typeof(T) == typeof(TimeSpan)) return 24;
        if (typeof(T) == typeof(Guid)) return 40;

        return 32;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static U8String FormatUnsized<T>(T value)
        where T : IUtf8SpanFormattable
    {
        int length;
        var buffer = new byte[64];
        while (!value.TryFormat(buffer, out length, default, CultureInfo.InvariantCulture))
        {
            buffer = new byte[buffer.Length * 2];
        }

        return new(buffer, 0, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static U8String FormatUnsized<T>(
        T value, ReadOnlySpan<char> format, IFormatProvider provider)
            where T : IUtf8SpanFormattable
    {
        int length;
        var buffer = new byte[64];
        while (!value.TryFormat(buffer, out length, format, provider))
        {
            buffer = new byte[buffer.Length * 2];
        }

        return new(buffer, 0, length);
    }
}
