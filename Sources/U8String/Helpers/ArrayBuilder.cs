using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace U8;

[InlineArray(Length)]
internal struct InlineBuffer64
{
    public const int Length = 64;

    byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element0, Length);
    }
}

[InlineArray(Length)]
internal struct InlineBuffer376
{
    // To account for alignment with byte[] in ArrayBuilder
    public const int Length = 376;

    byte _element0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element0, Length);
    }
}

// TODO: Rewrite to neuecc-style multi-pooled array builder
internal struct ArrayBuilder : IDisposable
{
    InlineBuffer376 _inline;
    byte[]? _array;

    public int BytesWritten { get; private set; }

    // TODO: add overload with initial capacity
    public ArrayBuilder()
    {
        Unsafe.SkipInit(out _inline);
    }

    public Span<byte> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_array ?? _inline.AsSpan()).SliceUnsafe(0, BytesWritten);
    }

    public Span<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_array ?? _inline.AsSpan()).SliceUnsafe(BytesWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        var free = Free;
        if (free.Length <= 0)
        {
            Grow();
        }

        free.AsRef() = value;
        BytesWritten++;
    }

    public void Write<T>(T value, ReadOnlySpan<char> format, IFormatProvider provider)
        where T : IUtf8SpanFormattable
    {
    Retry:
        if (value.TryFormat(Free, out var written, format, provider))
        {
            BytesWritten += written;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> span)
    {
    Retry:
        var free = Free;
        if (free.Length >= span.Length)
        {
            span.CopyToUnsafe(ref free.AsRef());
            BytesWritten += span.Length;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> span, int reserve)
    {
        Debug.Assert(reserve >= span.Length);

    Retry:
        var free = Free;
        if ((uint)free.Length >= (uint)reserve)
        {
            span.CopyToUnsafe(ref free.AsRef());
            BytesWritten += span.Length;
            return;
        }

        Grow();
        goto Retry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUnchecked(byte value)
    {
        Free.AsRef() = value;
        BytesWritten++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUnchecked(ReadOnlySpan<byte> span)
    {
        span.CopyToUnsafe(ref Free.AsRef());
        BytesWritten += span.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    void Grow()
    {
        const int initialRentLength = 1024;

        var arrayPool = ArrayPool<byte>.Shared;
        var rented = _array;
        var written = (rented ?? _inline.AsSpan())
            .SliceUnsafe(0, BytesWritten);

        var newLength = rented is null
            ? initialRentLength
            : rented.Length * 2;

        var newArr = arrayPool.Rent(newLength);

        written.CopyToUnsafe(ref newArr.AsRef());
        _array = newArr;

        if (rented != null)
        {
            arrayPool.Return(rented);
        }
    }

    public readonly void Dispose()
    {
        var array = _array;
        if (array != null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
