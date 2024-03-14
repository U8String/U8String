using System.ComponentModel;
using System.Net.WebSockets;

namespace U8.IO;

public static partial class U8WriteExtensions
{
    internal readonly struct WebSocketWriteable(
        WebSocket websocket,
        WebSocketMessageType type,
        WebSocketMessageFlags flags) : IWriteable
    {
        public void Write(ReadOnlySpan<byte> value)
        {
            throw new NotSupportedException("WebSocket does not support synchronous I/O.");
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken ct)
        {
            return websocket.SendAsync(value, type, flags, ct);
        }

        public void WriteDispose(ref InlineU8Builder builder)
        {
            throw new NotSupportedException("WebSocket does not support synchronous I/O.");
        }

        public async ValueTask WriteDisposeAsync(PooledU8Builder builder, CancellationToken ct)
        {
            await websocket
                .SendAsync(builder.WrittenMemory, type, flags, ct)
                .ConfigureAwait(false);
            
            builder.Dispose();
        }
    }

    public static ValueTask SendAsync(
        this WebSocket websocket,
        U8String value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        return websocket.SendAsync(value, type, flags, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask SendAsync(
        this WebSocket websocket,
        PooledU8Builder handler,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        return WriteAsync(new WebSocketWriteable(websocket, type, flags), handler, ct);
    }

    public static ValueTask SendAsync<T>(
        this WebSocket websocket,
        T value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default) where T : IUtf8SpanFormattable
    {
        return WriteAsync(new WebSocketWriteable(websocket, type, flags), value, ct);
    }

    public static ValueTask SendLineAsync(
        this WebSocket websocket,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        return websocket.SendAsync(U8Constants.NewLine, type, flags, ct);
    }

    public static ValueTask SendLineAsync(
        this WebSocket websocket,
        U8String value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        return WriteLineAsync(new WebSocketWriteable(websocket, type, flags), value, ct);
    }

    public static ValueTask SendLineAsync(
        this WebSocket websocket,
        ReadOnlyMemory<byte> value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        U8String.Validate(value.Span);
        return WriteLineAsync(new WebSocketWriteable(websocket, type, flags), value, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask SendLineAsync(
        this WebSocket websocket,
        PooledU8Builder handler,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default)
    {
        return WriteLineAsync(new WebSocketWriteable(websocket, type, flags), handler, ct);
    }

    public static ValueTask SendLineAsync<T>(
        this WebSocket websocket,
        T value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default) where T : IUtf8SpanFormattable
    {
        return WriteLineAsync(new WebSocketWriteable(websocket, type, flags), value, ct);
    }
}

public static partial class U8WriteEnumExtensions
{
    public static ValueTask SendAsync<T>(
        this WebSocket websocket,
        T value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default) where T : struct, Enum
    {
        return WriteAsync(new U8WriteExtensions.WebSocketWriteable(websocket, type, flags), value, ct);
    }

    public static ValueTask SendLineAsync<T>(
        this WebSocket websocket,
        T value,
        WebSocketMessageType type = WebSocketMessageType.Text,
        WebSocketMessageFlags flags = WebSocketMessageFlags.EndOfMessage,
        CancellationToken ct = default) where T : struct, Enum
    {
        return WriteLineAsync(new U8WriteExtensions.WebSocketWriteable(websocket, type, flags), value, ct);
    }
}