using System.Diagnostics;
using System.Text;

using U8.Primitives;
using U8.Shared;

namespace U8.IO;

public enum U8ReadResult
{
    Success = -1,
    EndOfStream = 0,
    InvalidOffset = 1,
    InvalidUtf8 = 2,
}

public class U8Reader : IDisposable
{
    readonly Stream _stream;
    readonly byte[] _buffer;

    int _bytesRead;
    int _bytesConsumed;

    public U8Reader(Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        _stream = stream;
        _buffer = new byte[4096];
    }

    public U8String? Read(int length, bool roundOffsets = false)
    {
        // var bytes = new byte[(nint)(uint)length + 1];
        // var buffer = bytes.AsSpan();
        // var bytesRead = _stream.Read(buffer);

        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String? ReadLine()
    {
        var line = ReadTo((byte)'\n');
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
    }

    public async ValueTask<U8String?> ReadLineAsync(CancellationToken ct = default)
    {
        var line = await ReadToAsync((byte)'\n', ct);
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(byte delimiter)
    {
        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadTo(char delimiter)
    {
        ThrowHelpers.CheckSurrogate(delimiter);

        return char.IsAscii(delimiter)
            ? ReadTo((byte)delimiter)
            : ReadTo(delimiter <= 0x7ff ? delimiter.AsTwoBytes() : delimiter.AsThreeBytes());
    }

    public U8String? ReadTo(Rune delimiter)
    {
        return delimiter.IsAscii
            ? ReadTo((byte)delimiter.Value)
            : ReadTo(delimiter.Value switch
            {
                <= 0x7ff => delimiter.AsTwoBytes(),
                <= 0xffff => delimiter.AsThreeBytes(),
                _ => delimiter.AsFourBytes()
            });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(ReadOnlySpan<byte> delimiter)
    {
        if (delimiter.Length <= 0)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadToAny(ReadOnlySpan<byte> delimiters)
    {
        if (delimiters.IsEmpty)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOfAny(delimiters);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOfAny(delimiters)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public ValueTask<U8String?> ReadToAsync(byte delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(char delimiter, CancellationToken ct = default)
    {
        ThrowHelpers.CheckSurrogate(delimiter);

        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(Rune delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(U8String delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    async ValueTask<U8String?> ReadToAsyncCore<T>(T delimiter, CancellationToken ct)
        where T : struct
    {
        Debug.Assert(delimiter is not U8String s || !s.IsEmpty);

        var unread = await FillAsync(ct);
        if (unread.Length == 0)
        {
            return null;
        }

        var (index, length) = U8Searching.IndexOf(unread.Span, delimiter);
        if (index >= 0)
        {
            Advance(index + length);
            return new(unread.Span.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread.Span);

        while ((unread = await FillAsync(ct)).Length > 0)
        {
            (index, length) = U8Searching.IndexOf(unread.Span, delimiter);

            if (index >= 0)
            {
                Advance(index + length);
                builder.AppendBytes(unread.Span.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread.Span);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadToEnd()
    {
        throw new NotImplementedException();
    }

    public U8ReadResult TryRead(int length, out U8String value)
    {
        throw new NotImplementedException();
    }

    void Advance(int length)
    {
        _bytesConsumed += length;
    }

    Span<byte> Fill()
    {
        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto Done;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        _bytesRead = read += _stream.Read(buffer, read, buffer.Length - read);
        _bytesConsumed = consumed;

    Done:
        return buffer.AsSpan(consumed, read - consumed);
    }

    async ValueTask<Memory<byte>> FillAsync(CancellationToken ct)
    {
        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto Done;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        _bytesRead = read += await _stream.ReadAsync(
            buffer.AsMemory(read, buffer.Length - read), ct);
        _bytesConsumed = consumed;

    Done:
        return buffer.AsMemory(consumed, read - consumed);
    }

    public void Dispose()
    {
        _stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
