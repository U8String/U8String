using System.Buffers;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

using U8.Shared;

namespace U8.IO;

public enum U8ReadResult
{
    Success = -1,
    EndOfStream = 0,
    InvalidOffset = 1,
    InvalidUtf8 = 2,
}

public readonly record
struct U8FileSource(SafeFileHandle Value) : IDisposable
{
    public void Dispose() => Value.Dispose();
}

public readonly record
struct U8StreamSource(Stream Value) : IDisposable
{
    public void Dispose() => Value.Dispose();
}

// TODO: remove all unchecked slicing to make it more resilient to concurrent misuse
// (as in, it is still UB but it should not be AVEing by reading random memory)
public partial class U8Reader<TSource>(TSource source) : IDisposable
    where TSource : struct, IDisposable
{
    const int BufferSize = 8192;

    readonly TSource _source = source;

    byte[] _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    long _offset; // Sentinel value of -1 indicates EOF
    int _bytesRead;
    int _bytesConsumed;
    bool _isBufferStolen;

    // TODO: Scenarios like .ReadLines().Take(1) are really hurt by
    // this implementation. Is it possible to special case them?
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
    void AdvanceSource(int length)
    {
        if (_source is U8FileSource)
        {
            _offset += length;
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
            _buffer = new byte[BufferSize];
            _bytesConsumed = 0;
            _bytesRead = 0;
            _isBufferStolen = false;
        }
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

        _source.Dispose();
        GC.SuppressFinalize(this);
    }
}
