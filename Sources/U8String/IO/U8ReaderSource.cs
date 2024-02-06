using System.Net.Sockets;
using System.Net.WebSockets;

using Microsoft.Win32.SafeHandles;

namespace U8.IO;

public interface IU8ReaderSource : IDisposable
{
    // TODO: See U8WebSocketSource TODO but the general idea is to
    // check all sources to ensure that the last segment/line is
    // "drained" by enumerators/Read{*} methods when the source
    // has successfully done reading and been closed/completed.
    int Read(long readerOffset, Span<byte> buffer);
    ValueTask<int> ReadAsync(
        long readerOffset,
        Memory<byte> buffer,
        CancellationToken ct);
}

// We don't have explicit extension support yet so using this workaround for now.
public readonly struct U8FileSource(SafeFileHandle handle) : IU8ReaderSource
{
    /* readonly bool _isSeekable; <-- TODO: Implement this */

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
public readonly struct U8SocketSource(Socket socket) : IU8ReaderSource
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long _, Span<byte> buffer)
    {
        return socket.Receive(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<int> ReadAsync(long _, Memory<byte> buffer, CancellationToken ct)
    {
        return socket.ReceiveAsync(buffer, ct);
    }

    public void Dispose() => socket.Dispose();
}

public readonly struct U8WebSocketSource(WebSocket socket) : IU8ReaderSource
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(long _, Span<byte> buffer)
    {
        throw new NotSupportedException();
    }

    // TODO: How to handle buffer closure in an idiomatic way?
    // The goal is to avoid forcing the user to explicitly "drain" the reader
    // and enable plain "await foreach" scenarios.
    // Note: it does appear to work correctly when WS is closed gracefully.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<int> ReadAsync(long _, Memory<byte> buffer, CancellationToken ct)
    {
        var response = await socket.ReceiveAsync(buffer, ct);
        return response.MessageType switch
        {
            WebSocketMessageType.Text => response.Count,
            WebSocketMessageType.Binary => response.Count,
            WebSocketMessageType.Close or _ => 0,
        };
    }

    public void Dispose() => socket.Dispose();
}
