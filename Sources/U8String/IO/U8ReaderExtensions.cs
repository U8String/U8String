using System.Net.WebSockets;

namespace U8.IO;

public static class U8StreamExtensions
{
    public static U8Reader<U8StreamSource> AsU8Reader(this Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(stream));
    }

    public static U8Reader<U8WebSocketSource> AsU8Reader(this WebSocket ws, bool disposeSource = false)
    {
        ThrowHelpers.CheckNull(ws);

        return new(new(ws), disposeSource);
    }

    public static U8LineReader<U8StreamSource> ReadU8Lines(this Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(new(stream)));
    }

    // TODO: naming
    // - AsUnowned
    // - DoNotDispose
    // - WithoutDispos(e/ing/al)
    // - SkipDispose
    public static U8LineReader<T> WithoutDisposing/* al? */<T>(this U8LineReader<T> reader)
        where T : struct, IU8ReaderSource
    {
        return new(reader.Value, disposeReader: false);
    }

    public static U8SplitReader<T, TSeparator> WithoutDisposing<T, TSeparator>(this U8SplitReader<T, TSeparator> reader)
        where T : struct, IU8ReaderSource
        where TSeparator : struct
    {
        return new(reader.Value, reader.Separator, disposeReader: false);
    }
}
