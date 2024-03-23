using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace U8.IO;

public interface IU8ReaderSource : IDisposable
{
    // TODO: See U8WebSocketSource TODO but the general idea is to
    // check all sources to ensure that the last segment/line is
    // "drained" by enumerators/Read{*} methods when the source
    // has successfully done reading and been closed/completed.
    int Read(long readerOffset, Span<byte> buffer);
    ValueTask<int> ReadAsync(long readerOffset, Memory<byte> buffer, CancellationToken ct);
}

// TODO: I don't like that this abstraction leaks/bothers the users
// with implementation details "just because", mostly to deal with
// the type system. Figure out a way to hide it without sacrificing
// the async cost savings.
public interface IU8SegmentedReaderSource<TSegment> : IU8ReaderSource
{
    internal readonly struct ReadResult(int length, bool endOfSegment)
    {
        public int Length => length;
        public bool EndOfSegment => endOfSegment;
    }

    U8SegmentReadResult GetReadResult(long readerOffset, TSegment segment);
    // TODO: This is not segment but rather a read result
    ValueTask<TSegment> ReadSegment(long readerOffset, Memory<byte> buffer, CancellationToken ct);
}

// TODO: Tentative API
public readonly struct U8SegmentReadResult(int length, bool endOfSegment, bool lastRead)
{
    public readonly int Length = length;
    public readonly bool EndOfSegment = endOfSegment;
    public readonly bool LastRead = lastRead;
}

// We don't have explicit extension support yet so using this workaround for now.
public readonly struct U8FileSource(SafeFileHandle handle) : IU8ReaderSource
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long readerOffset, Span<byte> buffer)
    {
        return RandomAccess.Read(handle, buffer, readerOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(long readerOffset, Memory<byte> buffer, CancellationToken ct)
    {
        return RandomAccess.ReadAsync(handle, buffer, readerOffset, ct);
    }

    public void Dispose() => handle.Dispose();
}

public readonly struct U8StreamSource(Stream stream) : IU8ReaderSource
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long _, Span<byte> buffer)
    {
        return stream.Read(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(long _, Memory<byte> buffer, CancellationToken ct)
    {
        return stream.ReadAsync(buffer, ct);
    }

    public void Dispose() => stream.Dispose();
}

// TODO: Does this make sense? Especially without non-owning reader support
// and different behavior on EOF vs zero-length reads?
public readonly struct U8SocketSource(
    Socket socket, SocketFlags flags = SocketFlags.None) : IU8ReaderSource
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long _, Span<byte> buffer)
    {
        return socket.Receive(buffer, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(long _, Memory<byte> buffer, CancellationToken ct)
    {
        return socket.ReceiveAsync(buffer, flags, ct);
    }

    public void Dispose() => socket.Dispose();
}

// TODO: Reconsider design once ReadToAny(byte, byte) is implemented
// TODO: Also consider special-casing implementation because the source
// memory is seekable and readable so we can skip bufferring the data.
public unsafe readonly struct U8MemorySource : IU8ReaderSource
{
    readonly byte* _ptr;
    readonly nint _length;

    public U8MemorySource(byte* ptr, nint length)
    {
        ArgumentNullException.ThrowIfNull(ptr);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        _ptr = ptr;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long readerOffset, Span<byte> buffer)
    {
        Debug.Assert(readerOffset >= 0);

        var available = int.CreateSaturating(_length - readerOffset);
        var readLength = Math.Min(available, buffer.Length);

        new ReadOnlySpan<byte>(_ptr + readerOffset, readLength)
            .CopyToUnsafe(ref buffer.AsRef());
        return readLength;
    }

    public ValueTask<int> ReadAsync(long readerOffset, Memory<byte> buffer, CancellationToken ct)
    {
        throw new NotSupportedException($"{nameof(U8MemorySource)} does not support asynchronous reads.");
    }

    public void Dispose()
    {
        NativeMemory.Free(_ptr);
    }
}

// TODO: WebSocket is a buffered source. This causes double-buffering that U8Reader tries hard
// to avoid. However, WebSocket.Options allows to override read/write buffer with a custom one.
// Investigate how to best integrate this with U8Reader with the following considerations:
// - U8Reader+U8WebSocketSource is not an exclusive owner of WebSocket and may not be an exclusive
// reader of it either - this may be a necessary (conservative) default
// - What is the cost and implications of swapping a read buffer of an active connection to
// allow buffer stealing technique to continue working even with this kind of source?
// - What are the overall best defaults for this? Perhaps warn the users that wrapping websocket in a
// reader makes it an exclusive owner of the read operations?
public readonly struct U8WebSocketSource(WebSocket socket) : IU8SegmentedReaderSource<ValueWebSocketReceiveResult>
{
    // TODO: Yet another note on importance of diasmbiguating EOF vs EOM
    public U8SegmentReadResult GetReadResult(long _, ValueWebSocketReceiveResult segment)
    {
        // For now just don't include the close message?
        return new(
            segment.Count,
            segment.EndOfMessage,
            segment.MessageType is WebSocketMessageType.Close);
    }

    public int Read(long _, Span<byte> buffer)
    {
        throw new NotSupportedException(
            "WebSocket does not support synchronous reads. Use asynchronous I/O instead.");
    }

    // TODO: How to handle buffer closure in an idiomatic way?
    // The goal is to avoid forcing the user to explicitly "drain" the reader
    // and enable plain "await foreach" scenarios.
    // Note: it does appear to work correctly when WS is closed gracefully.
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public async ValueTask<int> ReadAsync(long _, Memory<byte> buffer, CancellationToken ct)
    {
        // TODO: Detect the condition where we filled up the buffer up to
        // its capacity and we need to continue reading in a way that works
        // nicely with IU8SegmentedReaderSource.
        // i.e.: if (response.EndOfMessage) { /* ... */ }
        // perhaps ValueTask<(int Length, bool EndOfRead)> ?
        // UPD: Done, see IU8SegmentedReaderSource<T>
        var response = await socket.ReceiveAsync(buffer, ct);
        return response.MessageType switch
        {
            WebSocketMessageType.Text => response.Count,
            WebSocketMessageType.Binary => response.Count,
            // TODO: This potentially hides the close message contents
            // from the user, is there a nice yet generic way to handle it?
            WebSocketMessageType.Close or _ => 0,
        };
    }

    public ValueTask<ValueWebSocketReceiveResult> ReadSegment(long _, Memory<byte> buffer, CancellationToken ct)
    {
        return socket.ReceiveAsync(buffer, ct);
    }

    public void Dispose() => socket.Dispose();
}
