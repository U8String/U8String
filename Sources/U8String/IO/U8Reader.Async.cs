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

        // TODO: This is really bad for state machine object size.
        // Replace with U8Builder or similar once available.
        var builder = new InterpolatedU8StringHandler(unread.Length);
        AdvanceReader(unread.Length);
        builder.AppendBytes(unread.Span);

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
                AdvanceReader(index + length);
                builder.AppendBytes(unread[..index].Span);
                break;
            }

            AdvanceReader(unread.Length);
            builder.AppendBytes(unread.Span);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }
}