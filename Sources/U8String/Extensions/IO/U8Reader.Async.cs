using System.Diagnostics;
using System.Text;

using U8.Shared;

namespace U8.IO;

public partial class U8Reader<TSource>
{
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTask<U8String?> ReadToAsync<T>(T delimiter, CancellationToken ct)
    {
        return delimiter switch
        {
            byte b => ReadToAsync(b, ct),
            char c => ReadToAsync(c, ct),
            Rune r => ReadToAsync(r, ct),
            U8String s => ReadToAsync(s, ct),
            _ => throw new NotSupportedException()
        };
    }

    // TODO: Okay, so it *is* the async depth and yield cost that ruins perfomance.
    // TODO: !Do not use! This is completely broken until aligned with ReadTo implementation.
    async ValueTask<U8String?> ReadToAsyncCore<T>(T delimiter, CancellationToken ct)
        where T : struct
    {
        Debug.Assert(delimiter is not U8String s || !s.IsEmpty);

        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;
        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto SkipFirstRead;
        }

        // Check if we need to reset the buffer
        if (read >= buffer!.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        var filled = await ReadSourceAsync(
            buffer.AsMemory(read, buffer.Length - read), ct);
        AdvanceSource(filled);

        _bytesRead = read += filled;
        _bytesConsumed = consumed;

    SkipFirstRead:
        var unread = buffer.AsMemory(consumed, read - consumed);
        if (unread.Length <= 0)
        {
            return null;
        }

        var (index, length) = U8Searching.IndexOf(unread.Span, delimiter);
        if (index >= 0)
        {
            AdvanceReader(index + length);
            return new(unread.Span.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        AdvanceReader(unread.Length);
        builder.AppendBytes(unread.Span);

        while (true)
        {
            read = _bytesRead;
            consumed = _bytesConsumed;

            if (consumed < read)
            {
                // Nothing to fill, the reader must consume
                // the unread bytes first.
                goto SkipRead;
            }

            // Check if we need to reset the buffer
            if (read >= buffer!.Length)
            {
                Debug.Assert(consumed == read);
                read = 0;
                consumed = 0;
            }

            filled = await ReadSourceAsync(
                buffer.AsMemory(read, buffer.Length - read), ct);

            _bytesRead = read += filled;
            _bytesConsumed = consumed;

        SkipRead:
            unread = buffer.AsMemory(consumed, read - consumed);

            if (unread.Length <= 0)
            {
                break;
            }

            (index, length) = U8Searching.IndexOf(unread.Span, delimiter);

            if (index >= 0)
            {
                AdvanceReader(index + length);
                builder.AppendBytes(unread.Span.SliceUnsafe(0, index));
                break;
            }

            AdvanceReader(unread.Length);
            builder.AppendBytes(unread.Span);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ValueTask<int> ReadSourceAsync(Memory<byte> buffer, CancellationToken ct)
    {
        return _source switch
        {
            U8StreamSource stream => stream.Value.ReadAsync(buffer, ct),
            U8FileSource file => RandomAccess.ReadAsync(file.Value, buffer, _offset, ct),
            _ => throw new NotSupportedException()
        };
    }
}