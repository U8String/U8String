using System.Diagnostics;
using System.Text;

using U8.Primitives;

namespace U8.IO;

public partial class U8Reader<TSource>
{
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(byte delimiter)
    {
        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var unread = Fill();
        var isEOF = false;

    RetryCheckEOF:
        var index = unread.IndexOf(delimiter);
        if (index >= 0 || isEOF)
        {
            index = index >= 0
                ? index : unread.Length;

            // FIXME: This should be a bug in regards to opportunistic
            // null-termination where a single byte past the slice length
            // is effectively "unowned" and may be written to by the subsequent
            // read operation which could lead to terrible consequences when
            // passed as a C string IFF the buffer was filled exactly up to the
            // lentgh of the currently returned slice. Solve this by detecting this
            // edge case and shifting both bytes read and consumed by 1.
            // Otherwise, if that byte was already written to, then we could continue
            // relying on existing null-termination checks since the bytes in the
            // stolen buffer are written to exactly once.
            var result = ShouldStealBuffer(delimiter, index)
                ? new U8String(_buffer, _bytesConsumed, index)
                : new U8String(unread.SliceUnsafe(0, index), skipValidation: true);

            AdvanceReader(index + 1, isEOF);

            U8String.Validate(result);
            return result;
        }
        else if (unread is [])
        {
            return null;
        }
        // Buffer is null when return value of fill is []
        else if (_bytesRead < _buffer!.Length)
        {
            unread = FillCheckEOF(out isEOF);
            goto RetryCheckEOF;
        }

        var builder = new InlineU8Builder(unread.Length);
        builder.AppendBytes(unread);
        AdvanceReader(unread.Length);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                AdvanceReader(index + 1);
                break;
            }

            builder.AppendBytes(unread);
            AdvanceReader(unread.Length);
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
        var unread = Fill();
        var isEOF = false;

    RetryCheckEOF:
        var index = unread.IndexOf(delimiter);
        if (index >= 0 || isEOF)
        {
            index = index >= 0
                ? index : unread.Length;

            var result = ShouldStealBuffer(delimiter, index)
                ? new U8String(_buffer, _bytesConsumed, index)
                : new U8String(unread.SliceUnsafe(0, index), skipValidation: true);

            AdvanceReader(index + delimiter.Length, isEOF);

            U8String.Validate(result);
            return result;
        }
        else if (unread is [])
        {
            return null;
        }
        // Buffer is null when return value of fill is []
        else if (_bytesRead < _buffer!.Length)
        {
            unread = FillCheckEOF(out isEOF);
            goto RetryCheckEOF;
        }

        var builder = new InlineU8Builder(unread.Length);
        builder.AppendBytes(unread);
        AdvanceReader(unread.Length);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                AdvanceReader(index + delimiter.Length);
                break;
            }

            builder.AppendBytes(unread);
            AdvanceReader(unread.Length);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String? ReadTo<T>(T delimiter)
        where T : struct
    {
        return delimiter switch
        {
            byte b => ReadTo(b),
            char c => ReadTo(c),
            Rune r => ReadTo(r),
            U8String s => ReadTo(s.AsSpan()),
            _ => throw new NotSupportedException()
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadToAny(ReadOnlySpan<byte> delimiters)
    {
        if (delimiters.IsEmpty)
        {
            ThrowHelpers.ArgumentException();
        }

        throw new NotImplementedException();
    }

    public U8String ReadToEnd()
    {
        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var unread = Fill();
        if (_isBufferStolen || (
            unread.Length < _buffer!.Length &&
            unread.Length >= BufferSize / 2))
        {
            _isBufferStolen = true;

            var result = new U8String(_buffer, _bytesConsumed, unread.Length);

            AdvanceReader(unread.Length);

            U8String.Validate(result);
            return result;
        }

        var builder = new InlineU8Builder(unread.Length);
        builder.AppendBytes(unread);
        AdvanceReader(unread.Length);

        while ((unread = Fill()).Length > 0)
        {
            builder.AppendBytes(unread);
            AdvanceReader(unread.Length);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8ReadResult TryRead(int length, out U8String value)
    {
        throw new NotImplementedException();
    }

    // TODO: How does this interact with e.g. TcpStream and Slowloris attacks?
    // TODO: Support "stealing" the buffer with an adaptive strategy.
    // Likely when the read has fully populated it and it contains
    // delimiters or desired length/offset past 3/4 of its capacity.
    // Additionally, when EOF is reached it can be stolen if the difference
    // between sliced length and its size is below some reasonable threshold,
    // Maybe it's okay to just keep around the 4KB buffer for some odd 200B string?
    Span<byte> Fill()
    {
        // Bail out early if we done reading
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

            var filled = _source.Read(
                _offset, buffer.AsSpan(read, buffer.Length - read));
            _offset += filled;
            _bytesRead = read += filled;
            _bytesConsumed = consumed;

        Done:
            return buffer.AsSpan(consumed, read - consumed);
        }

        return [];
    }

    Span<byte> FillCheckEOF(out bool isEOF)
    {
        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        Debug.Assert(_offset >= 0);
        Debug.Assert(consumed < read);
        Debug.Assert(read < buffer.Length);

        var filled = _source.Read(
            _offset, buffer.AsSpan(read, buffer.Length - read));
        _offset += filled;
        _bytesRead = read += filled;

        isEOF = filled <= 0;
        return buffer.AsSpan(consumed, read - consumed);
    }
}