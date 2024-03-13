using System.Net.Sockets;
using System.Net.WebSockets;

using Microsoft.Win32.SafeHandles;

namespace U8.IO;

public static class U8ReaderExtensions
{
    public static U8Reader<U8StreamSource> AsU8Reader(this Stream stream, bool disposeSource)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(stream), disposeSource);
    }

    public static U8Reader<U8FileSource> AsU8Reader(this SafeFileHandle handle, bool disposeSource)
    {
        ThrowHelpers.CheckNull(handle);

        return new(new(handle), disposeSource);
    }

    public static U8Reader<U8SocketSource> AsU8Reader(
        this Socket socket, bool disposeSource, SocketFlags flags = SocketFlags.None)
    {
        ThrowHelpers.CheckNull(socket);

        return new(new(socket, flags), disposeSource);
    }

    public static U8Reader<U8WebSocketSource> AsU8Reader(
        this WebSocket ws, bool disposeSource)
    {
        ThrowHelpers.CheckNull(ws);

        return new(new(ws), disposeSource);
    }

    public static U8LineReader<U8StreamSource> ReadU8Lines(this Stream stream, bool disposeSource)
    {
        ThrowHelpers.CheckNull(stream);

        var source = new U8StreamSource(stream);
        var reader = new U8Reader<U8StreamSource>(source, disposeSource);

        return new(reader, disposeReader: true);
    }

    public static U8LineReader<U8FileSource> ReadU8Lines(this SafeFileHandle handle, bool disposeSource)
    {
        ThrowHelpers.CheckNull(handle);

        var source = new U8FileSource(handle);
        var reader = new U8Reader<U8FileSource>(source, disposeSource);

        return new(reader, disposeReader: true);
    }

    public static U8LineReader<U8SocketSource> ReadU8Lines(
        this Socket socket, bool disposeSource, SocketFlags flags = SocketFlags.None)
    {
        ThrowHelpers.CheckNull(socket);

        var source = new U8SocketSource(socket, flags);
        var reader = new U8Reader<U8SocketSource>(source, disposeSource);

        return new(reader, disposeReader: true);
    }

    public static U8LineReader<U8WebSocketSource> ReadU8Lines(
        this WebSocket ws, bool disposeSource)
    {
        ThrowHelpers.CheckNull(ws);

        var source = new U8WebSocketSource(ws);
        var reader = new U8Reader<U8WebSocketSource>(source, disposeSource);

        return new(reader, disposeReader: true);
    }

    public static U8WebSocketMessageReader ReadU8Messages(
        this WebSocket ws, bool disposeSource)
    {
        ThrowHelpers.CheckNull(ws);

        var source = new U8WebSocketSource(ws);
        var reader = new U8Reader<U8WebSocketSource>(source, disposeSource);

        return new(reader, disposeReader: true);
    }

    // public static U8SegmentReader<U8WebSocketSource> ReadU8Messages(
    //     this WebSocket ws,
    //     bool disposeSource)
    // {
    //     ThrowHelpers.CheckNull(ws);
    //
    //     var source = new U8WebSocketSource(ws);
    //     var reader = new U8Reader<U8WebSocketSource>(source, disposeSource);
    //
    //     return new(reader);
    // }

    // TODO: naming
    // - AsUnowned
    // - DoNotDispose
    // - WithoutDispos(e/ing/al)
    // - SkipDispose
    public static U8LineReader<T> WithoutDisposing/* al? */<T>(this U8LineReader<T> reader)
        where T : IU8ReaderSource
    {
        return new(reader.Value, disposeReader: false);
    }

    public static U8SplitReader<T, TSeparator> WithoutDisposing<T, TSeparator>(this U8SplitReader<T, TSeparator> reader)
        where T : IU8ReaderSource
        where TSeparator : struct
    {
        return new(reader.Value, reader.Separator, disposeReader: false);
    }

    public static U8SegmentReader<T, TSegment> WithoutDisposing<T, TSegment>(this U8SegmentReader<T, TSegment> reader)
        where T : IU8SegmentedReaderSource<TSegment>
    {
        return new(reader.Value, disposeReader: false);
    }
}
