using System.Diagnostics;
using System.Text;

using U8.Shared;

namespace U8.IO;

public partial class U8Reader<TSource>
{
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public async ValueTask<U8String?> ReadLineAsync(CancellationToken ct = default)
    {
        var line = await ReadToAsync((byte)'\n', ct);
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    ValueTask<U8String?> ReadToAsyncCore<T>(T delimiter, CancellationToken ct)
        where T : struct
    {
        Debug.Assert(delimiter is not U8String s || !s.IsEmpty);

        var read = TryReadBuffered(delimiter, out var success);

        return success
            ? new ValueTask<U8String?>(read)
            : ReadToAsyncCore2(delimiter, ct);
    }

    // TODO: Okay, so it *is* the async depth and yield cost that ruins perfomance.
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    async ValueTask<U8String?> ReadToAsyncCore2<T>(T delimiter, CancellationToken ct)
        where T : struct
    {
        Debug.Assert(delimiter is not U8String s || !s.IsEmpty);

        // FillAsync()
        Memory<byte> unread;
        {
            if (_offset >= 0)
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

                var filled = await _source.ReadAsync(
                    _offset, buffer.AsMemory(read, buffer.Length - read), ct);
                _offset += filled;
                _bytesRead = read += filled;
                _bytesConsumed = consumed;

            Done:
                unread = buffer.AsMemory(consumed, read - consumed);
            }
            else
            {
                unread = default;
            }
        }

        var isEOF = false;

    RetryCheckEOF:
        var (index, length) = U8Searching.IndexOf(unread.Span, delimiter);
        if (index >= 0 || isEOF)
        {
            index = index >= 0
                ? index : unread.Length;

            // FIXME: See the bug description in the sync version
            var result = ShouldStealBuffer(delimiter, index)
                ? new U8String(_buffer, _bytesConsumed, index)
                : new U8String(unread[..index].Span, skipValidation: true);

            AdvanceReader(index + length, isEOF);

            U8String.Validate(result);
            return result;
        }
        else if (unread.IsEmpty)
        {
            return null;
        }
        // Buffer is null when return value of fill is []
        else if (_bytesRead < _buffer!.Length)
        {
            // FillCheckEOFAsync()
            {
                var buffer = _buffer;
                var read = _bytesRead;
                var consumed = _bytesConsumed;

                Debug.Assert(_offset >= 0);
                Debug.Assert(consumed < read);
                Debug.Assert(read < buffer.Length);

                var filled = await _source.ReadAsync(
                    _offset, buffer.AsMemory(read, buffer.Length - read), ct);
                _offset += filled;
                _bytesRead = read += filled;

                isEOF = filled <= 0;
                unread = buffer.AsMemory(consumed, read - consumed);
            }
            goto RetryCheckEOF;
        }

        var builder = new PooledU8Builder(unread.Length);
        builder.AppendBytes(unread.Span);
        AdvanceReader(unread.Length);

        while (true)
        {
            // FillAsync()
            {
                if (_offset >= 0)
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

                    var filled = await _source.ReadAsync(
                        _offset, buffer.AsMemory(read, buffer.Length - read), ct);

                    _offset += filled;
                    _bytesRead = read += filled;
                    _bytesConsumed = consumed;

                Done:
                    unread = buffer.AsMemory(consumed, read - consumed);
                }
                else
                {
                    unread = default;
                }
            }

            if (unread.IsEmpty) break;

            (index, length) = U8Searching.IndexOf(unread.Span, delimiter);

            if (index >= 0)
            {
                builder.AppendBytes(unread[..index].Span);
                AdvanceReader(index + length);
                break;
            }

            builder.AppendBytes(unread.Span);
            AdvanceReader(unread.Length);
        }

        var fromBuilder = new U8String(builder.Written);
        builder.Dispose();
        return fromBuilder;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public async ValueTask<U8String?> ReadSegmentAsync<T, TSegment>(CancellationToken ct = default)
        where T : TSource, IU8SegmentedReaderSource<TSegment>
    {
        if (!Unread.IsEmpty)
        {
            goto ContinueReading;
        }
        else if (EOF)
        {
            return null;
        }

        var segmentRead = await ((IU8SegmentedReaderSource<TSegment>)_source)
            .ReadSegment(_offset, Free, ct);

        var readResult = ((IU8SegmentedReaderSource<TSegment>)_source)
            .GetReadResult(_offset, segmentRead);

        _offset += readResult.Length;
        _bytesRead += readResult.Length;

        if (readResult.EndOfSegment)
        {
            if (readResult.LastRead) EOF = true;

            var segment = readResult.Length > 0
                ? new U8String(Unread)
                : (U8String?)null;

            // It's a mess - I need to refactor the inner structure
            AdvanceReader(Unread.Length);
            return segment;
        }

    ContinueReading:
        var builder = new PooledU8Builder();
        builder.AppendBytes(Unread);
        AdvanceReader(Unread.Length);

        do
        {
            segmentRead = await ((IU8SegmentedReaderSource<TSegment>)_source)
                .ReadSegment(_offset, Free, ct);

            readResult = ((IU8SegmentedReaderSource<TSegment>)_source)
                .GetReadResult(_offset, segmentRead);

            _offset += readResult.Length;
            _bytesRead += readResult.Length;

            builder.AppendBytes(Unread);
            AdvanceReader(Unread.Length);
        } while (!readResult.EndOfSegment && !readResult.LastRead && readResult.Length > 0);

        if (readResult.LastRead) EOF = true;

        var result = new U8String(builder.Written);
        builder.Dispose();
        return result;
    }
}
