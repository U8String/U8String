using System.Buffers;
using System.Text;

namespace U8Primitives;

[InterpolatedStringHandler]
public struct U8InterpolatedStringHandler
{
    readonly IFormatProvider? _provider;
    InlineBuffer128 _inline;
    byte[]? _rented;

    public int BytesWritten { get; private set; }

    public Span<byte> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_rented ?? _inline.AsSpan()).SliceUnsafe(0, BytesWritten);
    }

    public Span<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_rented ?? _inline.AsSpan()).SliceUnsafe(BytesWritten);
    }

    public U8InterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        IFormatProvider? formatProvider = null)
    {
        Unsafe.SkipInit(out _inline);

        var initialLength = literalLength + formattedCount * 12;
        if (initialLength > InlineBuffer128.Length)
        {
            _rented = ArrayPool<byte>.Shared.Rent(initialLength);
        }

        _provider = formatProvider;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AppendLiteral(string s)
    {
    Retry:
        if (Encoding.UTF8.TryGetBytes(s, Free, out var written))
        {
            BytesWritten += written;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void AppendFormatted<T>(T value, ReadOnlySpan<char> format = default)
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
    void Grow()
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
            arrayPool.Return(rented, clearArray: true);
        }
    }

    internal readonly void Dispose()
    {
        var rented = _rented;
        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
        }
    }
}

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static U8String Format(ref U8InterpolatedStringHandler handler)
    {
        var result = new U8String(handler.Written, skipValidation: true);
        handler.Dispose();
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryFormatPresized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider, out U8String result)
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
        if (typeof(T) == typeof(byte)) return 8;
        if (typeof(T) == typeof(char)) return 8;
        if (typeof(T) == typeof(Rune)) return 8;
        if (typeof(T) == typeof(sbyte)) return 8;
        if (typeof(T) == typeof(ushort)) return 8;
        if (typeof(T) == typeof(short)) return 8;
        if (typeof(T) == typeof(uint)) return 16;
        if (typeof(T) == typeof(int)) return 16;
        if (typeof(T) == typeof(ulong)) return 24;
        if (typeof(T) == typeof(long)) return 24;
        if (typeof(T) == typeof(float)) return 16;
        if (typeof(T) == typeof(double)) return 24;
        if (typeof(T) == typeof(decimal)) return 32;
        if (typeof(T) == typeof(DateTime)) return 32;
        if (typeof(T) == typeof(DateTimeOffset)) return 40;
        if (typeof(T) == typeof(TimeSpan)) return 24;
        if (typeof(T) == typeof(Guid)) return 40;
        else return 32;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static U8String FormatUnsized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider)
            where T : IUtf8SpanFormattable
    {
        // TODO: Maybe it's okay to steal from array pool?
        int length;
        var buffer = new byte[64];
        while (!value.TryFormat(buffer, out length, format, provider))
        {
            buffer = new byte[buffer.Length * 2];
        }

        return new(buffer, 0, length);
    }
}
