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

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
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

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send(this Socket socket, ref InlineU8Builder handler, SocketFlags flags = SocketFlags.None)
    {
        WriteBuilder(new SocketWriteable(socket, flags), ref handler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : IUtf8SpanFormattable
    {
        WriteUtf8Formattable(new SocketWriteable(socket, flags), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask SendAsync(
        this Socket socket, PooledU8Builder handler, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteBuilderAsync(new SocketWriteable(socket, flags), handler, ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask SendAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteUtf8FormattableAsync(new SocketWriteable(socket, flags), value, ct);
    }

    public static void SendLine(this Socket socket, SocketFlags flags = SocketFlags.None)
    {
        new SocketWriteable(socket, flags).Write(NewLine);
    }

    public static void SendLine(this Socket socket, U8String value, SocketFlags flags = SocketFlags.None)
    {
        WriteLineSpan(new SocketWriteable(socket, flags), value);
    }

    public static void SendLine(this Socket socket, ReadOnlySpan<byte> value, SocketFlags flags = SocketFlags.None)
    {
        U8String.Validate(value);
        WriteLineSpan(new SocketWriteable(socket, flags), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SendLine(this Socket socket, ref InlineU8Builder handler, SocketFlags flags = SocketFlags.None)
    {
        WriteLineBuilder(new SocketWriteable(socket, flags), ref handler);
    }

    public static void SendLine<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : IUtf8SpanFormattable
    {
        WriteLineUtf8Formattable(new SocketWriteable(socket, flags), value);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return new SocketWriteable(socket, flags).WriteAsync(U8Constants.NewLine, ct);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, U8String value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteLineMemoryAsync(new SocketWriteable(socket, flags), value, ct);
    }

    public static ValueTask SendLineAsync(
        this Socket socket, ReadOnlyMemory<byte> value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        U8String.Validate(value.Span);
        return WriteLineMemoryAsync(new SocketWriteable(socket, flags), value, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask SendLineAsync(
        this Socket socket, PooledU8Builder handler, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
    {
        return WriteLineBuilderAsync(new SocketWriteable(socket, flags), handler, ct);
    }

    public static ValueTask SendLineAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteLineUtf8FormattableAsync(new SocketWriteable(socket, flags), value, ct);
    }
}

public static partial class U8WriteEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Send<T>(this Socket socket, T value, SocketFlags flags = SocketFlags.None)
        where T : struct, Enum
    {
        WriteEnum(new U8WriteExtensions.SocketWriteable(socket, flags), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask SendAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteEnumAsync(new U8WriteExtensions.SocketWriteable(socket, flags), value, ct);
    }

    public static void SendLine<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None)
            where T : struct, Enum
    {
        WriteLineEnum(new U8WriteExtensions.SocketWriteable(socket, flags), value);
    }

    public static ValueTask SendLineAsync<T>(
        this Socket socket, T value, SocketFlags flags = SocketFlags.None, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteLineEnumAsync(new U8WriteExtensions.SocketWriteable(socket, flags), value, ct);
    }
}
