using System.Buffers;
using System.Diagnostics;

using U8.Shared;

namespace U8.IO;

public enum U8ReadResult
{
    Success = -1,
    EndOfStream = 0,
    InvalidOffset = 1,
    InvalidUtf8 = 2,
}

// TODO: remove all unchecked slicing to make it more resilient to concurrent misuse
// (as in, it is still UB but it should not be AVEing by reading random memory)
// TODO: decide whether to seal the class and if not - which methods to make virtual
public partial class U8Reader<TSource>(
    TSource source, bool disposeSource = true) : IDisposable
        where TSource : IU8ReaderSource
{
    const int BufferSize = 8192;

    readonly TSource _source = source;
    readonly bool _disposeSource = disposeSource;

    byte[] _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    long _offset; // Sentinel value of -1 indicates EOF
    int _bytesRead;
    int _bytesConsumed;
    bool _isBufferStolen;

    public ReadOnlySpan<byte> Buffered
    {
        get => _buffer.AsSpan(0, _bytesRead);
    }

    public ReadOnlySpan<byte> Unread
    {
        get => _buffer.AsSpan(_bytesConsumed, _bytesRead - _bytesConsumed);
    }

    internal Memory<byte> Free
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _buffer.AsMemory(_bytesRead);
    }

    internal bool EOF
    {
        get => _offset < 0;
        set
        {
            if (value)
            {
                _offset = -1;
            }
        }
    }

    // TODO: Scenarios like .ReadLines().Take(1) are really hurt by
    // this implementation. Is it possible to special case them?
    // TODO: This is yet another example of needing a Pattern<T>
    // abstraction for string scanning.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool ShouldStealBuffer<T>(T delimiter, int searchStart)
        where T : struct
    {
        // One of the following must be true:
        // - The buffer is already stolen
        // - EOF was definitely reached
        // - The search start is at or before 1/4 of the buffer
        // - The delimiter is at or past 3/4 of the buffer
        Debug.Assert((uint)searchStart < BufferSize);

        if (_isBufferStolen)
        {
            return true;
        }

        return Core(delimiter, searchStart);

        bool Core(T delimiter, int searchStart)
        {
            var buffer = _buffer;
            var read = _bytesRead;
            var consumed = _bytesConsumed;
            if (read >= BufferSize / 2)
            {
                // TODO: Edge cases are not compliant with the above
                // heuristic - fix them and tune as needed.
                if (read < BufferSize)
                {
                    // Assuming we reached EOF
                    _isBufferStolen = true;
                    return true;
                }

                searchStart += consumed;
                var lastPos = U8Searching.LastIndexOf(
                    buffer.AsSpan(searchStart, read - searchStart), delimiter).Offset;

                if (lastPos is >= 0 and >= (BufferSize / 2))
                {
                    _isBufferStolen = true;
                    return true;
                }
            }
            else if (read < buffer!.Length)
            {
                // This *might* be EOF but even if it's not,
                // we want to reallocate the buffer to an exact size
                // and just steal it unconditionally assuming one of the
                // following is true:
                // - We reached EOF for a small file or a short stream
                // - The size of the current transmission or chunk is
                // small enough we can just keep the downsized buffer around
                var length = read - consumed;
                var newBuffer = new byte[length + 1];
                buffer.AsSpan(consumed, length).CopyTo(newBuffer);

                Debug.Assert(!_isBufferStolen);
                Debug.Assert(buffer.Length is BufferSize);
                _buffer = newBuffer;
                _bytesRead = length;
                _bytesConsumed = 0;
                _isBufferStolen = true;

                ArrayPool<byte>.Shared.Return(buffer);

                return true;
            }

            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool ShouldStealBuffer(ReadOnlySpan<byte> delimiter, int searchStart)
    {
        // One of the following must be true:
        // - The buffer is already stolen
        // - EOF was definitely reached
        // - The search start is at or before 1/4 of the buffer
        // - The delimiter is at or past 3/4 of the buffer
        Debug.Assert((uint)searchStart < BufferSize);

        if (_isBufferStolen)
        {
            return true;
        }

        return Core(delimiter, searchStart);

        bool Core(ReadOnlySpan<byte> delimiter, int searchStart)
        {
            var buffer = _buffer;
            var read = _bytesRead;
            var consumed = _bytesConsumed;
            if (read >= BufferSize / 2)
            {
                // TODO: Edge cases are not compliant with the above
                // heuristic - fix them and tune as needed.
                if (read < BufferSize)
                {
                    // Assuming we reached EOF
                    _isBufferStolen = true;
                    return true;
                }

                searchStart += consumed;
                var lastPos = buffer
                    .AsSpan(searchStart, read - searchStart)
                    .LastIndexOf(delimiter);

                if (lastPos is >= 0 and >= (BufferSize / 2))
                {
                    _isBufferStolen = true;
                    return true;
                }
            }
            // FIXME: This is wrong, when consuming e.g. WebSocket stream,
            // this would cause high buffer traffic to-from the pool because
            // it effectively resizes the buffer to exact read length *and then*
            // marks it as stolen, which would re-rent a new buffer on the next
            // read. This should not be happening and we should do one of these:
            // - Mark as stolen but don't resize to very short length, downsizing
            // the buffer instead to some reasonable minimum (e.g. 1KB)
            // - Don't mark as stolen and for short lengths just allocate a string
            // In addition, we should always look at the difference between the delimiter
            // offset and the actual read length - we don't do that now which should
            // allow to make the heuristic much smarter and avoid excessive pooling churn,
            // wasted allocations or memory due to rooting large buffers in returned strings.
            else if (read < buffer!.Length)
            {
                // This *might* be EOF but even if it's not,
                // we want to reallocate the buffer to an exact size
                // and just steal it unconditionally assuming one of the
                // following is true:
                // - We reached EOF for a small file or a short stream
                // - The size of the current transmission or chunk is
                // small enough we can just keep the downsized buffer around
                var length = read - consumed;
                var newBuffer = new byte[length + 1];
                buffer.AsSpan(consumed, length).CopyTo(newBuffer);

                Debug.Assert(!_isBufferStolen);
                Debug.Assert(buffer.Length is BufferSize);
                _buffer = newBuffer;
                _bytesRead = length;
                _bytesConsumed = 0;
                _isBufferStolen = true;

                ArrayPool<byte>.Shared.Return(buffer);

                return true;
            }

            return false;
        }
    }

    void AdvanceReader(int length, bool isEOF = false)
    {
        var consumed = _bytesConsumed + length;
        var read = _bytesRead;
        var isBufferStolen = _isBufferStolen;
        if (consumed < read)
        {
            _bytesConsumed = consumed;
        }
        else if (isEOF)
        {
            _offset = -1;
        }
        else if (!isBufferStolen)
        {
            _bytesConsumed = 0;
            _bytesRead = 0;
        }
        else if (_offset >= 0)
        {
            // Must not be reached if we done reading
            Debug.Assert(_buffer != null);
            Debug.Assert(_isBufferStolen);
            // Allocate a new array instead of stealing it from the pool,
            // if we stole it from the reader previously.
            // This also allows to give back a BufferSize array to the pool
            // instead of constantly draining it in some cases.
            _buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
            _bytesConsumed = 0;
            _bytesRead = 0;
            _isBufferStolen = false;
        }
    }

    public ReadOnlySpan<byte> Drain(int length = -1)
    {
        if (length < 0)
        {
            length = _bytesRead - _bytesConsumed;
        }

        // TODO: This can make it so that the memory observed by the caller
        // will be modified by the next read, reconsider whether to expose
        // this at all and replace with something like DrainTo(buffer).
        var result = _buffer.AsSpan(_bytesConsumed, length);
        AdvanceReader(length);
        return result;
    }

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null!);
        if (buffer != null
            && !_isBufferStolen
            && buffer.Length is BufferSize)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        _offset = -1;
        _bytesConsumed = 0;
        _bytesRead = 0;
        _isBufferStolen = false;

        if (_disposeSource)
        {
            _source.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
