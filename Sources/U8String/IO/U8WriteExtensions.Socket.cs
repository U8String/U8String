using System.ComponentModel;
using System.Net.Sockets;

namespace U8.IO;

public static partial class U8WriteExtensions
{
    internal readonly struct SocketWriteable(Socket socket, SocketFlags flags) : IWriteable
    {
        public void Write(ReadOnlySpan<byte> value)
        {
            do
            {
                var written = socket.Send(value, flags);
                value = value[written..];
            } while (value.Length > 0);
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken ct)
        {
            do
            {
                var written = await socket
                    .SendAsync(value, flags, ct)
                    .ConfigureAwait(false);

                value = value[written..];
            } while (value.Length > 0);
        }

        public void WriteDispose(ref InlineU8Builder builder)
        {
            var value = builder.Written;
            do
            {
                var written = socket.Send(value, flags);
                value = value[written..];
            } while (value.Length > 0);
            builder.Dispose();
        }

        public async ValueTask WriteDisposeAsync(PooledU8Builder builder, CancellationToken ct)
        {
            var value = builder.WrittenMemory;
            do
            {
                var written = await socket
                    .SendAsync(value, flags, ct)
                    .ConfigureAwait(false);

                value = value[written..];
            } while (value.Length > 0);
            builder.Dispose();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Send(this Socket socket, ref InlineU8Builder handler, SocketFlags flags = SocketFlags.None)
    {
        Write(new SocketWriteable(socket, flags), ref handler);
    }

    public static void Send<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : IUtf8SpanFormattable
    {
        Write(new SocketWriteable(socket, flags), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask SendAsync(
        this Socket socket, PooledU8Builder handler, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteAsync(new SocketWriteable(socket, flags), handler, ct);
    }

    public static ValueTask SendAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteAsync(new SocketWriteable(socket, flags), value, ct);
    }

    public static void SendLine(this Socket socket, SocketFlags flags = SocketFlags.None)
    {
        new SocketWriteable(socket, flags).Write(NewLine);
    }

    public static void SendLine(this Socket socket, U8String value, SocketFlags flags = SocketFlags.None)
    {
        WriteLine(new SocketWriteable(socket, flags), value);
    }

    public static void SendLine(this Socket socket, ReadOnlySpan<byte> value, SocketFlags flags = SocketFlags.None)
    {
        U8String.Validate(value);
        WriteLine(new SocketWriteable(socket, flags), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SendLine(this Socket socket, ref InlineU8Builder handler, SocketFlags flags = SocketFlags.None)
    {
        WriteLine(new SocketWriteable(socket, flags), ref handler);
    }

    public static void SendLine<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : IUtf8SpanFormattable
    {
        WriteLine(new SocketWriteable(socket, flags), value);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return new SocketWriteable(socket, flags).WriteAsync(U8Constants.NewLine, ct);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, U8String value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteLineAsync(new SocketWriteable(socket, flags), value, ct);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, ReadOnlyMemory<byte> value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        U8String.Validate(value.Span);
        return WriteLineAsync(new SocketWriteable(socket, flags), value, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask SendLineAsync(
        this Socket socket, PooledU8Builder handler, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteAsync(new SocketWriteable(socket, flags), handler, ct);
    }

    public static ValueTask SendLineAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteLineAsync(new SocketWriteable(socket, flags), value, ct);
    }
}

public static partial class U8WriteEnumExtensions
{
    public static void Send<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : struct, Enum
    {
        Write(new U8WriteExtensions.SocketWriteable(socket, flags), value);
    }

    public static ValueTask SendAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteAsync(new U8WriteExtensions.SocketWriteable(socket, flags), value, ct);
    }

    public static void SendLine<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None)
            where T : struct, Enum
    {
        WriteLine(new U8WriteExtensions.SocketWriteable(socket, flags), value);
    }

    public static ValueTask SendLineAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteLineAsync(new U8WriteExtensions.SocketWriteable(socket, flags), value, ct);
    }
}