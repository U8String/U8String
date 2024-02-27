using System.Buffers;
using System.Collections.Concurrent;
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
#pragma warning disable RCS1003 // Add braces to multi-line expression. Why: more compact and readable here.
#pragma warning disable IDE0038, RCS1220 // Use pattern matching. Why: non-boxing interface resolution on structs.
// TODO: Review the impact on async methods (with consideration of upcoming async2)
// and write analyzers to prevent most common copy-by-value mistakes.
public /* ref */ struct InterpolatedU8StringHandler
{
    InlineBuffer128 _inline;
    readonly IFormatProvider? _provider;
    byte[]? _rented;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InterpolatedU8StringHandler()
    {
        Unsafe.SkipInit(out _inline);
        _provider = CultureInfo.InvariantCulture;
    }

    public InterpolatedU8StringHandler(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        Unsafe.SkipInit(out _inline);

        if (length > InlineBuffer128.Length)
        {
            _rented = ArrayPool<byte>.Shared.Rent(length);
        }

        _provider = CultureInfo.InvariantCulture;
    }

    public InterpolatedU8StringHandler(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider = null)
    {
        Unsafe.SkipInit(out _inline);

        var initialLength = literalLength + (formattedCount * 12);
        if (initialLength > InlineBuffer128.Length)
        {
            _rented = ArrayPool<byte>.Shared.Rent(initialLength);
        }

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
                return;
            }

            AppendConstantString(s);
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

    // Explicit no-format overload for more compact codegen
    // and specialization so that *if* TryFormat is inlined into
    // the body, the format-specific branches are optimized away.
    // TODO: Remove unbound generic and replace enum, byte[] and
    // IA<byte> formatting with extension methods workaround.
    public void AppendFormatted<T>(T value)
    {
    Retry:
        if (value is IUtf8SpanFormattable)
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
            var formattable = new EnumU8StringFormat<T>(value);
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
        else if (typeof(T) == typeof(U8String))
        {
            AppendFormatted((U8String)(object)value!);
            return;
        }
        else if (typeof(T) == typeof(U8String?))
        {
            AppendFormatted((U8String?)(object)value!);
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
    internal void EnsureCapacity(int length)
    {
        if (Free.Length < length)
        {
            Grow(length);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Grow()
    {
        const int initialRentLength = 1024;

        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _rented;
        var written = (rented ?? _inline.AsSpan())
            .SliceUnsafe(0, BytesWritten);

        var newLength = rented is null
            ? initialRentLength
            : rented.Length * 2;

        var newArr = arrayPool.Rent(newLength);

        written.CopyToUnsafe(ref newArr.AsRef());
        _rented = newArr;

        if (rented != null)
        {
            arrayPool.Return(rented);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Grow(int hint)
    {
        const int initialRentLength = 1024;

        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _rented;
        var written = (rented ?? _inline.AsSpan())
            .SliceUnsafe(0, BytesWritten);

        var newLength = rented is null
            ? initialRentLength
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

    [DoesNotReturn, StackTraceHidden]
    static void UnsupportedAppend<T>()
    {
        throw new NotSupportedException(
            $"\nCannot append a value of type '{typeof(T)}' which does not implement '{typeof(IUtf8SpanFormattable)}' or is not '{typeof(Enum)}' or '{typeof(byte[])}'.");
    }
}

readonly struct EnumU8StringFormat<T>(T value) // : IUtf8SpanFormattable
    where T : notnull //, struct, Enum
{
    readonly static ConcurrentDictionary<T, byte[]> Cache = [];

    // This counter can be very imprecise but that's acceptable, we only
    // need to limit the cache size to some reasonable amount, tracking
    // the exact number of entries is not necessary.
    static uint Count;
    const int MaxCapacity = 1024;

    public bool TryFormat(Span<byte> destination, out int bytesWritten)
    {
        var bytes = GetBytes(value);
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
        var bytes = GetBytes(value);
        return new(bytes, 0, bytes.Length - 1);
    }

    public static implicit operator EnumU8StringFormat<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static byte[] GetBytes(T value)
    {
        if (Cache.TryGetValue(value, out var cached))
        {
            return cached;
        }

        return Add(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static byte[] Add(T value)
    {
        var utf16 = value.ToString();
        var length = Encoding.UTF8.GetByteCount(utf16!);
        var bytes = new byte[length + 1];

        Encoding.UTF8.GetBytes(utf16, bytes);

        var count = Count;
        if (count <= MaxCapacity)
        {
            Count = count + 1;
            Cache[value] = bytes;
        }

        return bytes;
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
