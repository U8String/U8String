using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace U8Primitives;

[InlineArray(Size)]
internal struct InlineBuffer128
{
    public const int Size = 128;

    byte _element0;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte AsRef(int index)
    {
        return ref _element0.Add(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element0, Size);
    }
}

[InlineArray(Size)]
internal struct InlineBuffer256
{
    public const int Size = 256;

    byte _element0;

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte AsRef(int index)
    {
        return ref _element0.Add(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element0, Size);
    }
}

internal struct ArrayBuilder : IDisposable
{
    InlineBuffer256 _inline;
    byte[]? _array;

    public int BytesWritten { get; private set; }

    public Span<byte> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_array is null ? _inline.AsSpan() : _array.AsSpan()).SliceUnsafe(0, BytesWritten);
    }

    public Span<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_array is null ? _inline.AsSpan() : _array.AsSpan()).SliceUnsafe(BytesWritten);
    }

    public void Write(byte value)
    {
        if (Free.Length <= 0)
        {
            Grow();
        }

        Free.AsRef() = value;
        BytesWritten++;
    }

    public void Write<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        int written;
        while (!value.TryFormat(Free, out written, format, provider))
        {
            Grow();
        }

        BytesWritten += written;
    }

    public void Write(ReadOnlySpan<byte> span)
    {
        if (span.Length > 0)
        {
            if (TryWriteInline(span))
            {
                return;
            }

            while (!TryWriteArray(span))
            {
                Grow();
            }
        }
    }

    bool TryWriteInline(ReadOnlySpan<byte> span)
    {
        if ((uint)span.Length <= (uint)(InlineBuffer256.Size - BytesWritten))
        {
            span.CopyToUnsafe(ref _inline.AsRef(BytesWritten));
            BytesWritten += span.Length;
            return true;
        }

        return false;
    }

    bool TryWriteArray(ReadOnlySpan<byte> span)
    {
        if (_array != null && (
            (uint)span.Length <= (uint)(_array.Length - BytesWritten)))
        {
            span.CopyToUnsafe(ref _array.AsRef(BytesWritten));
            BytesWritten += span.Length;
            return true;
        }

        return false;
    }

    void Grow()
    {
        var arrayPool = ArrayPool<byte>.Shared;
        if (_array is null)
        {
            var next = arrayPool.Rent(InlineBuffer256.Size * 2);

            _inline.AsSpan().CopyToUnsafe(ref next.AsRef());
            _array = next;
        }
        else
        {
            var last = _array;
            var length = last.Length * 2;
            var next = arrayPool.Rent(length);

            last.AsSpan().CopyToUnsafe(ref next.AsRef());
            _array = next;

            arrayPool.Return(last, clearArray: true);
        }
    }

    public readonly void Dispose()
    {
       if (_array != null)
       {
           ArrayPool<byte>.Shared.Return(_array, clearArray: true);
       }
    }
}
